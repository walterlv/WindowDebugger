﻿using Lsj.Util.Text;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.Extensions;
using Lsj.Util.Win32.Structs;
using Lsj.Util.WPF;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static Lsj.Util.Win32.Enums.SetWindowPosFlags;
using static Lsj.Util.Win32.Kernel32;
using static Lsj.Util.Win32.User32;
using static Lsj.Util.Win32.Enums.ProcessAccessRights;
using static Lsj.Util.Win32.Constants;
using System.IO;
using Lsj.Util.Win32.BaseTypes;

namespace WindowDebugger.ViewModels
{
    public class WindowItem : ModelObject
    {
        private IntPtr _windowHandle;
        public IntPtr WindowHandle { get => _windowHandle; set => SetField(ref _windowHandle, value, extraAction: RefreshItem); }

        private string _lastError;
        public string LastError { get => _lastError; set => SetField(ref _lastError, value); }

        private string _text;
        public string Text { get => _text; set => SetText(value); }

        private WindowStyles _styles;
        public WindowStyles Styles { get => _styles; set => SetStyles(value); }

        private WindowStylesEx _stylesEx;
        public WindowStylesEx StylesEx { get => _stylesEx; set => SetStylesEx(value); }

        private ClassStyles _classStyles;
        public ClassStyles ClassStyles { get => _classStyles; set => SetClassStyles(value); }

        private int _processID;
        public int ProcessID { get => _processID; }

        private string _processName;
        public string ProcessName { get => _processName; }

        private string _className;
        public string ClassName { get => _className; }

        private int _left;
        public int Left { get => _left; set => SetField(ref _left, value); }

        private int _top;
        public int Top { get => _top; set => SetField(ref _top, value); }

        private int _width;
        public int Width { get => _width; set => SetField(ref _width, value); }

        private int _height;
        public int Height { get => _height; set => SetField(ref _height, value); }

        private ShowWindowCommands _windowShowStates;
        public ShowWindowCommands WindowShowStates { get => _windowShowStates; set => SetWindowShowStates(value); }

        private void SetError()
        {
            LastError = ErrorMessageExtensions.GetSystemErrorMessageFromCode((uint)Marshal.GetLastWin32Error());
        }

        private void SetText(string value)
        {
            LastError = null;
            var result = SetWindowText(_windowHandle, value);
            if (!result)
            {
                SetError();
            }
            RefreshText();
        }

        private void SetStyles(WindowStyles value)
        {
            SetLastError(0);
            SetWindowLong(_windowHandle, GetWindowLongIndexes.GWL_STYLE, (IntPtr)value);
            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode != 0)
            {
                SetError();
            }
            RefreshStyles();
        }

        private void SetStylesEx(WindowStylesEx value)
        {
            SetLastError(0);
            SetWindowLong(_windowHandle, GetWindowLongIndexes.GWL_EXSTYLE, (IntPtr)value);
            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode != 0)
            {
                SetError();
            }
            RefreshStylesEx();
        }

        private void SetClassStyles(ClassStyles value)
        {
            SetLastError(0);
            SetClassLong(_windowHandle, GetClassLongIndexes.GCL_STYLE, (IntPtr)value);
            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode != 0)
            {
                SetError();
            }
            RefreshClassStyles();
        }

        public void SetTopMost(bool isTopMost)
        {
            var result = true;
            if (isTopMost)
            {
                result = SetForegroundWindow(_windowHandle);
                if (!result)
                {
                    SetError();
                }
            }

            if (result)
            {
                result = SetWindowPos(_windowHandle, isTopMost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                if (!result)
                {
                    SetError();
                }
            }

            RefreshStylesEx();
        }

        public bool SetWindowRect()
        {
            var result = SetWindowPos(WindowHandle, IntPtr.Zero, _left, _top, _width, _height, SWP_NOZORDER);
            if (!result)
            {
                SetError();
            }
            RefreshWindowRect();
            RefreshWindowPlacement();
            return result;
        }

        public void SetWindowShowStates(ShowWindowCommands commands)
        {
            ShowWindow(_windowHandle, commands);
            RefreshWindowPlacement();
        }

        public void RefreshItem()
        {
            RefreshText();
            RefreshStyles();
            RefreshStylesEx();
            RefreshClassStyles();
            RefreshProcessInformation();
            RefreshClassName();
            RefreshWindowRect();
            RefreshWindowPlacement();
        }

        public void RefreshText()
        {
            var sb = new StringBuilder(50);
            GetWindowText(_windowHandle, sb, 50);
            var text = sb.ToString();
            SetField(ref _text, text, propertyName: nameof(Text));
        }

        public void RefreshStyles()
        {
            var style = (WindowStyles)GetWindowLong(WindowHandle, GetWindowLongIndexes.GWL_STYLE).SafeToUInt32();
            SetField(ref _styles, style, propertyName: nameof(Styles));
        }

        public void RefreshStylesEx()
        {
            var style = (WindowStylesEx)GetWindowLong(WindowHandle, GetWindowLongIndexes.GWL_EXSTYLE).SafeToUInt32();
            SetField(ref _stylesEx, style, propertyName: nameof(StylesEx));
        }

        public void RefreshClassStyles()
        {
            var style = (ClassStyles)((UIntPtr)GetClassLong(WindowHandle, GetClassLongIndexes.GCL_STYLE)).SafeToUInt32();
            SetField(ref _classStyles, style, propertyName: nameof(ClassStyles));
        }

        public void RefreshProcessInformation()
        {
            GetWindowThreadProcessId(WindowHandle, out var processid);
            SetField(ref _processID, (int)processid, propertyName: nameof(ProcessID));

            //Process.GetProcessById((int)processid)?.ProcessName is too slow
            var process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processid);
            if (process != NULL)
            {
                var length = (DWORD)MAX_PATH;
                var path = new StringBuilder(MAX_PATH);
                if (QueryFullProcessImageName(process, 0, path, ref length))
                {
                    SetField(ref _processName, Path.ChangeExtension(Path.GetFileName(path.ToString()), null), propertyName: nameof(ProcessName));
                }
                CloseHandle(process);
            }
        }

        public void RefreshClassName()
        {
            var sb = new StringBuilder(100);
            if (GetClassName(WindowHandle, sb, 100) != 0)
            {
                SetField(ref _className, sb.ToString(), propertyName: nameof(ClassName));
            }
            else
            {
                SetError();
            }
        }

        public void RefreshWindowRect()
        {
            if (GetWindowRect(WindowHandle, out var rect))
            {
                Left = rect.left;
                Top = rect.top;
                Height = rect.bottom - rect.top;
                Width = rect.right - rect.left;
            }
        }

        public void RefreshWindowPlacement()
        {
            var placement = new WINDOWPLACEMENT();
            placement.length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>();
            if (GetWindowPlacement(WindowHandle, ref placement))
            {
                SetField(ref _windowShowStates, placement.showCmd, propertyName: nameof(WindowShowStates));
            }
        }

        public override string ToString() => $"0x{WindowHandle.ToString("X8")}{(!Text.IsNullOrEmpty() ? $"({Text})" : "")}";
    }
}
