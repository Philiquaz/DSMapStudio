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
        //TODO
        public static string ProductVersion = "NOWINFORMSVERSION";
#else
        public static string ProductVersion = WinForms.Application.ProductVersion;
#endif

        public static void Exit()
        {
#if NOWINFORMS
            //TODO
            Environment.Exit(0);
#else
            WinForms.Application.Exit();
#endif
        }
    }
    public class FolderBrowserDialog
    {
        public string Description = null;
        public bool UseDescriptionForTitle = false;

        public string SelectedPath;

        public DialogResult ShowDialog()
        {
#if NOWINFORMS
            //TODO
            return DialogResult.Cancel;
#else
            WinForms.FolderBrowserDialog dlg = new WinForms.FolderBrowserDialog();

            if (Description != null)
                dlg.Description = Description;
            if (UseDescriptionForTitle)
                dlg.UseDescriptionForTitle = UseDescriptionForTitle;

            var result = dlg.ShowDialog();
            SelectedPath = dlg.SelectedPath;
            switch (result)
            {
                case WinForms.DialogResult.Cancel:
                    return DialogResult.Cancel;
                case WinForms.DialogResult.OK:
                    return DialogResult.OK;
                case WinForms.DialogResult.Yes:
                    return DialogResult.Yes;
                case WinForms.DialogResult.No:
                    return DialogResult.No;
            }
            return DialogResult.Cancel;
        }
#endif
    }
    public class OpenFileDialog
    {
        public string Title = null;
        public string Filter = null;
        public bool ValidateNames = false;
        public bool CheckFileExists = false;
        public bool CheckPathExists = false;
        public bool Multiselect = false;

        public string FileName;
        public string[] FileNames;

        public DialogResult ShowDialog()
        {
#if NOWINFORMS
            //TODO
            return DialogResult.Cancel;
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
            switch (result)
            {
                case WinForms.DialogResult.Cancel:
                    return DialogResult.Cancel;
                case WinForms.DialogResult.OK:
                    return DialogResult.OK;
                case WinForms.DialogResult.Yes:
                    return DialogResult.Yes;
                case WinForms.DialogResult.No:
                    return DialogResult.No;
            }
            return DialogResult.Cancel;
#endif
        }
    }
    public class SaveFileDialog
    {
        public string Title = null;
        public string Filter = null;
        public bool ValidateNames = false;
        public bool CheckFileExists = false;
        public bool CheckPathExists = false;

        public string FileName;

        public DialogResult ShowDialog()
        {
#if NOWINFORMS
            //TODO
            return DialogResult.Cancel;
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
            switch (result)
            {
                case WinForms.DialogResult.Cancel:
                    return DialogResult.Cancel;
                case WinForms.DialogResult.OK:
                    return DialogResult.OK;
                case WinForms.DialogResult.Yes:
                    return DialogResult.Yes;
                case WinForms.DialogResult.No:
                    return DialogResult.No;
            }
            return DialogResult.Cancel;
#endif
        }
    }
    
    public class MessageBox
    {
        public static DialogResult Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.None)
        {
#if NOWINFORMS
            //TODO
            return DialogResult.Cancel;
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
            switch (result)
            {
                case WinForms.DialogResult.Cancel:
                    return DialogResult.Cancel;
                case WinForms.DialogResult.OK:
                    return DialogResult.OK;
                case WinForms.DialogResult.Yes:
                    return DialogResult.Yes;
                case WinForms.DialogResult.No:
                    return DialogResult.No;
            }
            return DialogResult.Cancel;
#endif
        }

    }
    public enum MessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo
    }
    public enum DialogResult
    {
        Cancel,
        OK,
        Yes,
        No
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
