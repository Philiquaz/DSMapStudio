#define NOWINFORMS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
#if NOWINFORMS
#else
using WinForms = System.Windows.Forms;
#endif

namespace StudioCore.Forms
{
    public class Forms
    {
#if NOWINFORMS
        public static string ProductVersion = "NOWINFORMSVERSION";
#else
        public static string ProductVersion = WinForms.Application.ProductVersion;
#endif

        public static void Exit()
        {
#if NOWINFORMS
            Environment.Exit(0);
#else
            WinForms.Application.Exit();
#endif
        }


        internal static (FolderBrowserDialog, Action<DialogResult>) fbd = (null, null);
        internal static (OpenFileDialog, Action<DialogResult>) ofd = (null, null);
        internal static (SaveFileDialog, Action<DialogResult>) sfd = (null, null);
        internal static (string, string, MessageBoxButtons, MessageBoxIcon, Action<DialogResult>) mb = (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null);

        public static void FormsPopups()
        {
#if NOWINFORMS
            bool open = true;
            if (fbd != (null, null))
            {
                ImGui.OpenPopup("FormsFolderBrowserDialog");
                if (ImGui.BeginPopupModal("FormsFolderBrowserDialog", ref open, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    if (fbd.Item1.Description != null)
                        ImGui.Text(fbd.Item1.Description);
                    ImGui.Text("Open Folder...");
                    ImGui.InputText("##FormsFolderBrowserDialogInput", ref fbd.Item1.SelectedPath, 1024);
                    if (ImGui.Button("Open"))
                    {
                        var func = fbd.Item2;
                        fbd = (null, null);
                        func(DialogResult.OK);
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        var func = fbd.Item2;
                        fbd = (null, null);
                        func(DialogResult.Cancel);
                    }
                    ImGui.EndPopup();
                }
                else
                {
                    var func = fbd.Item2;
                    fbd = (null, null);
                    func(DialogResult.Cancel);
                }
            }
            if (ofd != (null, null))
            {
                ImGui.OpenPopup("FormsOpenFileDialog");
                if (ImGui.BeginPopupModal("FormsOpenFileDialog", ref open, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    if (ofd.Item1.Title != null)
                        ImGui.Text(ofd.Item1.Title);
                    ImGui.Text("Open File...");
                    ImGui.InputText("##FormsOpenFileDialogInput", ref ofd.Item1.FileName, 1024);
                    if (ImGui.Button("Open"))
                    {
                        var func = ofd.Item2;
                        ofd = (null, null);
                        func(DialogResult.OK);
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        var func = ofd.Item2;
                        ofd = (null, null);
                        func(DialogResult.Cancel);
                    }
                    ImGui.EndPopup();
                }
                else
                {
                    var func = ofd.Item2;
                    ofd = (null, null);
                    func(DialogResult.Cancel);
                }
            }
            if (sfd != (null, null))
            {
                ImGui.OpenPopup("FormsSaveFileDialog");
                if (ImGui.BeginPopupModal("FormsSaveFileDialog", ref open, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    if (sfd.Item1.Title != null)
                        ImGui.Text(sfd.Item1.Title);
                    ImGui.Text("Save File...");
                    ImGui.InputText("##FormsSaveFileDialogInput", ref sfd.Item1.FileName, 1024);
                    if (ImGui.Button("Save"))
                    {
                        var func = sfd.Item2;
                        sfd = (null, null);
                        func(DialogResult.OK);
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        var func = sfd.Item2;
                        sfd = (null, null);
                        func(DialogResult.Cancel);
                    }
                    ImGui.EndPopup();
                }
                else
                {
                    var func = sfd.Item2;
                    sfd = (null, null);
                    func(DialogResult.Cancel);
                }
            }
            if (mb != (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null))
            {
                ImGui.OpenPopup("FormsMessageBox");
                if (ImGui.BeginPopupModal("FormsMessageBox", ref open, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text(mb.Item2);
                    ImGui.Text(mb.Item1);
                    if (mb.Item3 == MessageBoxButtons.YesNo)
                    {
                        if (ImGui.Button("Yes"))
                        {
                            var func = mb.Item5;
                            mb = (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null);
                            func(DialogResult.Yes);
                        }
                            ImGui.SameLine();
                        if (ImGui.Button("No"))
                        {
                            var func = mb.Item5;
                            mb = (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null);
                            func(DialogResult.No);
                        }
                    }
                    else
                    {
                        if (ImGui.Button("OK"))
                        {
                            var func = mb.Item5;
                            mb = (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null);
                            func(DialogResult.OK);
                        }
                        if (mb.Item3 == MessageBoxButtons.OKCancel)
                        {
                            ImGui.SameLine();
                            if (ImGui.Button("Cancel"))
                            {
                            var func = mb.Item5;
                            mb = (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null);
                            func(DialogResult.Cancel);
                            }
                        }
                    }
                    ImGui.EndPopup();
                }
                else
                {
                    var func = mb.Item5;
                    mb = (null, null, MessageBoxButtons.OK, MessageBoxIcon.None, null);
                    func(DialogResult.Cancel);
                }
            }
#else
#endif
        }
    }

    public class FolderBrowserDialog
    {
        public string Description = "";
        public bool UseDescriptionForTitle = false;

        public string SelectedPath;

        public void ShowDialog(Action<DialogResult> Callback)
        {
#if NOWINFORMS
            Forms.fbd = (this, Callback);
#else
            WinForms.FolderBrowserDialog dlg = new WinForms.FolderBrowserDialog();

            if (Description != null)
                dlg.Description = Description;
            if (UseDescriptionForTitle)
                dlg.UseDescriptionForTitle = UseDescriptionForTitle;

            var result = dlg.ShowDialog();
            SelectedPath = dlg.SelectedPath;
            Callback(result switch
            {
                WinForms.DialogResult.Cancel => DialogResult.Cancel,
                WinForms.DialogResult.OK => DialogResult.OK,
                WinForms.DialogResult.Yes => DialogResult.Yes,
                WinForms.DialogResult.No => DialogResult.No,
                _ => DialogResult.Cancel
            });
#endif
        }
    }
    public class OpenFileDialog
    {
        public string Title = "";
        public string Filter = "";
        public bool ValidateNames = false;
        public bool CheckFileExists = false;
        public bool CheckPathExists = false;
        public bool Multiselect = false;

        public string FileName = "";
        public string[] FileNames
        {
            get => new string[]{FileName};
        }

        public void ShowDialog(Action<DialogResult> Callback)
        {
#if NOWINFORMS
            Forms.ofd = (this, Callback);
#else
            WinForms.OpenFileDialog dlg = new WinForms.OpenFileDialog();

            if (Title != null)
                dlg.Title = Title;
            if (Filter != null)
                dlg.Filter = Filter;
            if (ValidateNames)
                dlg.ValidateNames = ValidateNames;
            if (CheckFileExists)
                dlg.CheckFileExists = CheckFileExists;
            if (CheckPathExists)
                dlg.CheckPathExists = CheckPathExists;
            if (Multiselect)
                dlg.Multiselect = Multiselect;

            var result = dlg.ShowDialog();
            FileName = dlg.FileName;
            FileNames = dlg.FileNames;
            Callback(result switch
            {
                WinForms.DialogResult.Cancel => DialogResult.Cancel,
                WinForms.DialogResult.OK => DialogResult.OK,
                WinForms.DialogResult.Yes => DialogResult.Yes,
                WinForms.DialogResult.No => DialogResult.No,
                _ => DialogResult.Cancel
            });
#endif
        }
    }
    public class SaveFileDialog
    {
        public string Title = "";
        public string Filter = "";
        public bool ValidateNames = false;
        public bool CheckFileExists = false;
        public bool CheckPathExists = false;

        public string FileName;

        public void ShowDialog(Action<DialogResult> Callback)
        {
#if NOWINFORMS
            Forms.sfd = (this, Callback);
#else
            WinForms.SaveFileDialog dlg = new WinForms.SaveFileDialog();

            if (Title != null)
                dlg.Title = Title;
            if (Filter != null)
                dlg.Filter = Filter;
            if (ValidateNames)
                dlg.ValidateNames = ValidateNames;
            if (CheckFileExists)
                dlg.CheckFileExists = CheckFileExists;
            if (CheckPathExists)
                dlg.CheckPathExists = CheckPathExists;

            var result = dlg.ShowDialog();
            FileName = dlg.FileName;
            Callback(result switch
            {
                WinForms.DialogResult.Cancel => DialogResult.Cancel,
                WinForms.DialogResult.OK => DialogResult.OK,
                WinForms.DialogResult.Yes => DialogResult.Yes,
                WinForms.DialogResult.No => DialogResult.No,
                _ => DialogResult.Cancel
            });
#endif
        }
    }
    
    public class MessageBox
    {
        public static void Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, Action<DialogResult> Callback = null)
        {
#if NOWINFORMS
            Forms.mb = (message, title, buttons, icon, Callback);
#else
            WinForms.MessageBoxButtons btn = buttons switch
            {
                MessageBoxButtons.OK => WinForms.MessageBoxButtons.OK,
                MessageBoxButtons.OKCancel => WinForms.MessageBoxButtons.OKCancel,
                MessageBoxButtons.YesNo => WinForms.MessageBoxButtons.YesNo
            };
            WinForms.MessageBoxIcon icn = icon switch
            {
                MessageBoxIcon.None => WinForms.MessageBoxIcon.None,
                MessageBoxIcon.Warning => WinForms.MessageBoxIcon.Warning,
                MessageBoxIcon.Error => WinForms.MessageBoxIcon.Error,
                MessageBoxIcon.Question => WinForms.MessageBoxIcon.Question,
                MessageBoxIcon.Information => WinForms.MessageBoxIcon.Information
            };
            var result = WinForms.MessageBox.Show(message, title, btn, icn);
            if (Callback != null)
                Callback(result switch
                {
                    WinForms.DialogResult.Cancel => DialogResult.Cancel,
                    WinForms.DialogResult.OK => DialogResult.OK,
                    WinForms.DialogResult.Yes => DialogResult.Yes,
                    WinForms.DialogResult.No => DialogResult.No,
                    _ => DialogResult.Cancel
                });
#endif
        }

    }
    public enum DialogResult
    {
        Cancel,
        OK,
        Yes,
        No
    }
    public enum MessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo
    }
    public enum MessageBoxIcon
    {
        None,
        Warning,
        Error,
        Question,
        Information
    }

}