#nullable enable
using Andre.Formats;
using DotNext.Collections.Generic;
using Org.BouncyCastle.Crypto.Engines;
using StudioCore.Editor;
using StudioCore.ParamEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StudioCore.Editor.MassEdit;

public enum MassEditResultType
{
    SUCCESS,
    PARSEERROR,
    OPERATIONERROR
}

public class MassEditResult
{
    public string Information;
    public MassEditResultType Type;

    public MassEditResult(MassEditResultType result, string info)
    {
        Type = result;
        Information = info;
    }
}

internal class MEParseException : Exception
{
    internal MEParseException(string? message, int line) : base($@"{message} (line {line})")
    {
    }
}
internal class MEOperationException : Exception
{
    internal MEOperationException(string? message) : base(message)
    {
    }
}

internal struct MEFilterStage
{
    internal string command;
    internal TypelessSearchEngine engine;
    // No arguments because this is handled separately in SearchEngine
    internal MEFilterStage(string toParse, int line, string stageName, TypelessSearchEngine stageEngine)
    {
        command = toParse.Trim();
        if (command.Equals(""))
        {
            throw new MEParseException($@"Could not find {stageName} filter. Add : and one of {string.Join(", ", stageEngine.AvailableCommandsForHelpText())}", line);
        }
        engine = stageEngine;
    }
}
internal struct MEOperationStage
{
    internal string command;
    internal string arguments;
    internal METypelessOperation operation;
    internal MEOperationStage(string toParse, int line, string stageName, METypelessOperation operationType)
    {
        var stage = toParse.TrimStart().Split(' ', 2);
        command = stage[0].Trim();
        if (stage.Length > 1)
            arguments = stage[1];
        if (command.Equals(""))
        {
            throw new MEParseException($@"Could not find operation to perform. Add : and one of {string.Join(' ', operationType.AllCommands().Keys)}", line);
        }
        if (!operationType.AllCommands().ContainsKey(command))
        {
            throw new MEParseException($@"Unknown {stageName} operation {command}", line);
        }
        operation = operationType;
    }
    internal string[] getArguments(int count)
    {
        return arguments.Split(':', count);
    }
}

public static class MassParamEdit
{
    internal static Dictionary<string, object> massEditVars = new();

    internal static object WithDynamicOf(object instance, Func<dynamic, object> dynamicFunc)
    {
        try
        {
            return Convert.ChangeType(dynamicFunc(instance), instance.GetType());
        }
        catch
        {
            // Second try, handle byte[], and casts from numerical values to string which need parsing.
            var ret = dynamicFunc(instance.ToParamEditorString());
            if (instance.GetType() == typeof(byte[]))
            {
                ret = ParamUtils.Dummy8Read((string)ret, ((byte[])instance).Length);
            }

            return Convert.ChangeType(ret, instance.GetType());
        }
    }

    internal static void AppendParamEditAction(this List<EditorAction> actions, Param.Row row,
        (PseudoColumn, Param.Column) col, object newval)
    {
        if (col.Item1 == PseudoColumn.ID)
        {
            if (!row.ID.Equals(newval))
            {
                actions.Add(new PropertiesChangedAction(row.GetType().GetProperty("ID"), -1, row, newval));
            }
        }
        else if (col.Item1 == PseudoColumn.Name)
        {
            if (row.Name == null || !row.Name.Equals(newval))
            {
                actions.Add(new PropertiesChangedAction(row.GetType().GetProperty("Name"), -1, row, newval));
            }
        }
        else
        {
            Param.Cell handle = row[col.Item2];
            if (!(handle.Value.Equals(newval)
                  || (handle.Value.GetType() == typeof(byte[])
                      && ParamUtils.ByteArrayEquals((byte[])handle.Value, (byte[])newval))))
            {
                actions.Add(new PropertiesChangedAction(handle.GetType().GetProperty("Value"), -1, handle, newval));
            }
        }
    }
}

public class MassParamEditRegex
{
    /* Line number associated with this edit command, for error reporting. */
    private int _currentLine;
    /* Current actions from execution of this edit command. */
    private List<EditorAction> _partialActions = new();

    private ParamBank bank;
    private ParamEditorSelectionState context;
    private object[] argFuncs;
    METypelessOperationDef parsedOp;

    Queue<MEFilterStage> filters = new();
    MEOperationStage operation;

    internal static ParamEditorSelectionState totalHackPleaseKillme = null;
    public static (MassEditResult, ActionManager child) PerformMassEdit(ParamBank bank, string commandsString,
        ParamEditorSelectionState context)
    {
        int currentLine = 0;
        try
        {
            var commands = commandsString.Split('\n');
            var changeCount = 0;
            ActionManager childManager = new();
            foreach (var cmd in commands)
            {
                currentLine++;
                var command = cmd;
                if (command.StartsWith("##") || string.IsNullOrWhiteSpace(command))
                {
                    continue;
                }

                if (command.EndsWith(';'))
                {
                    command = command.Substring(0, command.Length - 1);
                }

                MassParamEditRegex currentEditData = new();
                currentEditData._currentLine = currentLine;
                currentEditData.bank = bank;
                currentEditData.context = context;
                totalHackPleaseKillme = context;

                MassEditResult result = currentEditData.ParseAndExecCommand(command);

                if (result.Type != MassEditResultType.SUCCESS)
                {
                    return (result, null);
                }

                List<EditorAction> actions = currentEditData._partialActions;

                changeCount += actions.Count;
                childManager.ExecuteAction(new CompoundAction(actions));
            }

            return (new MassEditResult(MassEditResultType.SUCCESS, $@"{changeCount} cells affected"), childManager);
        }
        catch (Exception e)
        {
            return (new MassEditResult(MassEditResultType.PARSEERROR, e.ToString()), null);
        }
    }

    private MassEditResult ParseAndExecCommand(string command)
    {
        var stage = command.Split(":", 2);

        Type currentType = typeof((bool, bool)); // Always start at boolbool type as basis
        string firstStage = stage[0];
        string firstStageKeyword = firstStage.Trim().Split(" ", 2)[0];

        var op = METypelessOperation.GetEditOperation(currentType);
        // Try run an operation
        if (op != null && op.HandlesCommand(firstStageKeyword))
            return ParseOpStep(stage[1], op.NameForHelpTexts(), op);

        var nextStage = TypelessSearchEngine.GetSearchEngines(currentType);
        // Try out each defined search engine for the current type
        foreach ((TypelessSearchEngine engine, Type t) in nextStage)
        {
            if (engine.HandlesCommand(firstStageKeyword))
            {
                return ParseFilterStep(command, engine);
            }
        }
        //Assume it's default search of last search option
        return ParseFilterStep(command, nextStage.Last().Item1);
    }

    private MassEditResult ParseFilterStep(string stageText, TypelessSearchEngine expectedSearchEngine)
    {
        var stage = stageText.Split(":", 2);
        string stageName = expectedSearchEngine.NameForHelpTexts();
        filters.Enqueue(new MEFilterStage(stage[0], _currentLine, stageName, expectedSearchEngine));

        if (stage.Length < 2)
        {
            var esList = expectedSearchEngine.NextSearchEngines();
            var eo = expectedSearchEngine.NextOperation();
            if (esList.Any() && eo != null)
                return new MassEditResult(MassEditResultType.PARSEERROR, $@"Could not find {esList.Last().Item1.NameForHelpTexts()} filter or {eo.NameForHelpTexts()} operation to perform. Check your colon placement. (line {_currentLine})");
            if (esList.Any())
                return new MassEditResult(MassEditResultType.PARSEERROR, $@"Could not find {esList.Last().Item1.NameForHelpTexts()} filter. Check your colon placement. (line {_currentLine})");
            if (eo != null)
                return new MassEditResult(MassEditResultType.PARSEERROR, $@"Could not find {eo.NameForHelpTexts()} operation to perform. Check your colon placement. (line {_currentLine})");
            return new MassEditResult(MassEditResultType.PARSEERROR, $@"Could not find next stage to perform (no suggestions found). Check your colon placement. (line {_currentLine})");
        }

        Type currentType = expectedSearchEngine.getElementType();
        string restOfStages = stage[1];
        string nextStageKeyword = restOfStages.Trim().Split(" ", 2)[0];

        var op = METypelessOperation.GetEditOperation(currentType);
        // Try run an operation
        if (op != null && op.HandlesCommand(nextStageKeyword))
            return ParseOpStep(stage[1], op.NameForHelpTexts(), op);

        var nextStage = TypelessSearchEngine.GetSearchEngines(currentType);
        // Try out each defined search engine for the current type
        foreach ((TypelessSearchEngine engine, Type t) in nextStage)
        {
            if (engine.HandlesCommand(nextStageKeyword))
                return ParseFilterStep(restOfStages, engine);
        }
        //Assume it's default search of last search option
        return ParseFilterStep(restOfStages, nextStage.Last().Item1);
    }
    private MassEditResult ParseOpStep(string stageText, string stageName, METypelessOperation operation)
    {
        this.operation = new MEOperationStage(stageText, _currentLine, stageName, operation);

        parsedOp = operation.AllCommands()[this.operation.command];
        argFuncs = MEOperationArgument.arg.getContextualArguments(parsedOp.argNames.Length, this.operation.arguments);
        if (parsedOp.argNames.Length != argFuncs.Length)
        {
            return new MassEditResult(MassEditResultType.PARSEERROR, $@"Invalid number of arguments for operation {this.operation.command} (line {_currentLine})");
        }

        var contextObjects = new Dictionary<Type, (object, object)>() { { typeof(bool), (true, true)} };

        if (filters.Count == 0)
            return SandboxMassEditExecution(() => ExecOp(this.operation, this.operation.command, argFuncs, contextObjects, operation));
        else
        {
            MEFilterStage baseFilter = filters.Dequeue();
            return SandboxMassEditExecution(() => ExecStage(baseFilter, baseFilter.engine, (true, true), argFuncs, contextObjects));
        }
        throw new MEParseException("No initial stage or op was parsed", _currentLine);
    }

    private MassEditResult SandboxMassEditExecution(Func<MassEditResult> innerFunc)
    {
        try
        {
            return innerFunc();
        }
        catch (Exception e)
        {
            return new MassEditResult(MassEditResultType.OPERATIONERROR, @$"Error on line {_currentLine}" + '\n' + e.GetBaseException().ToString());
        }
    }

    private MassEditResult ExecStage(MEFilterStage info, TypelessSearchEngine engine, (object, object) contextObject, IEnumerable<object> argFuncs, Dictionary<Type, (object, object)> contextObjects)
    {
        var editCount = -1;
        foreach ((object, object) currentObject in engine.SearchNoType(contextObject, info.command, false, false))
        {
            editCount++;
            //add context
            contextObjects[engine.getElementType()] = currentObject;
            //update argGetters
            IEnumerable<object> newArgFuncs = argFuncs.Select((func, i) => func.tryFoldAsFunc(editCount, currentObject));
            //exec it
            MassEditResult res;

            if (filters.Count == 0)
                res = ExecOp(operation, operation.command, argFuncs, contextObjects, operation.operation);
            else
            {
                MEFilterStage nextFilterFilter = filters.Dequeue();
                res = ExecStage(nextFilterFilter, nextFilterFilter.engine, currentObject, newArgFuncs, contextObjects);
            }
            if (res.Type != MassEditResultType.SUCCESS)
            {
                return res;
            }
        }
        return new MassEditResult(MassEditResultType.SUCCESS, "");
    }
    private MassEditResult ExecOp(MEOperationStage opInfo, string opName, IEnumerable<object> argFuncs, Dictionary<Type, (object, object)> contextObjects, METypelessOperation opType)
    {
        var argValues = argFuncs.Select(f => f.assertCompleteContextOrThrow(0).ToParamEditorString()).ToArray();
        var opResults = parsedOp.function(opType.getTrueValue(contextObjects), argValues);
        if (!opType.validateResult(opResults))
        {
            return new MassEditResult(MassEditResultType.OPERATIONERROR, $@"Error performing {opName} operation {opInfo.command} (line {_currentLine})");
        }
        _partialActions.Add(null);//TODO using opType
        return new MassEditResult(MassEditResultType.SUCCESS, "");
    }
}

public class MassParamEditOther
{
    public static AddParamsAction SortRows(ParamBank bank, string paramName)
    {
        Param param = bank.Params[paramName];
        List<Param.Row> newRows = new(param.Rows);
        newRows.Sort((a, b) => { return a.ID - b.ID; });
        return new AddParamsAction(param, paramName, newRows, true,
            true); //appending same params and allowing overwrite
    }
}
