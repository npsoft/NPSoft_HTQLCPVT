﻿#pragma checksum "G:\samuel_photobookmark\packages\SilverlightFTP\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "CCED3E2E5BAFEB40D0581F776ABF108C"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace SilverlightFTP {
    
    
    public partial class MainPage : System.Windows.Controls.UserControl {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.TextBox TbHost;
        
        internal System.Windows.Controls.TextBox TbUser;
        
        internal System.Windows.Controls.PasswordBox TbPass;
        
        internal System.Windows.Controls.Button ConnectButton;
        
        internal System.Windows.Controls.Primitives.Popup popUp;
        
        internal System.Windows.Controls.Grid PopupLay;
        
        internal System.Windows.Controls.TextBlock CurrentNameTextBlock;
        
        internal System.Windows.Controls.TextBlock CurrentNameIsFolder;
        
        internal System.Windows.Controls.TextBlock NewNameTextBlock;
        
        internal System.Windows.Controls.TextBox EditTextBox;
        
        internal System.Windows.Controls.Button OkRenButton;
        
        internal System.Windows.Controls.Button CancelRenButton;
        
        internal System.Windows.Controls.Primitives.Popup ExistsPopUp;
        
        internal System.Windows.Controls.Grid ExistPopupLay;
        
        internal System.Windows.Controls.TextBlock ExistsText;
        
        internal System.Windows.Controls.Button OkExistsButton;
        
        internal System.Windows.Controls.Button CancelExistsButton;
        
        internal System.Windows.Controls.ListBox FileBox;
        
        internal System.Windows.Controls.Button CancelAllButton;
        
        internal System.Windows.Controls.ListBox ProgressBox;
        
        internal System.Windows.Controls.ListBox DownProgressBox;
        
        internal System.Windows.Controls.Button ASortingButton;
        
        internal System.Windows.Controls.Button SSortingButton;
        
        internal System.Windows.Controls.Button CopySelectedButton;
        
        internal System.Windows.Controls.Button PasteSelectedButton;
        
        internal System.Windows.Controls.Button CheckAllButton;
        
        internal System.Windows.Controls.Button PauseButton;
        
        internal System.Windows.Controls.Button CreateNewFolderButton;
        
        internal System.Windows.Controls.Button RefreshButton;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/SilverlightFTP;component/MainPage.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.TbHost = ((System.Windows.Controls.TextBox)(this.FindName("TbHost")));
            this.TbUser = ((System.Windows.Controls.TextBox)(this.FindName("TbUser")));
            this.TbPass = ((System.Windows.Controls.PasswordBox)(this.FindName("TbPass")));
            this.ConnectButton = ((System.Windows.Controls.Button)(this.FindName("ConnectButton")));
            this.popUp = ((System.Windows.Controls.Primitives.Popup)(this.FindName("popUp")));
            this.PopupLay = ((System.Windows.Controls.Grid)(this.FindName("PopupLay")));
            this.CurrentNameTextBlock = ((System.Windows.Controls.TextBlock)(this.FindName("CurrentNameTextBlock")));
            this.CurrentNameIsFolder = ((System.Windows.Controls.TextBlock)(this.FindName("CurrentNameIsFolder")));
            this.NewNameTextBlock = ((System.Windows.Controls.TextBlock)(this.FindName("NewNameTextBlock")));
            this.EditTextBox = ((System.Windows.Controls.TextBox)(this.FindName("EditTextBox")));
            this.OkRenButton = ((System.Windows.Controls.Button)(this.FindName("OkRenButton")));
            this.CancelRenButton = ((System.Windows.Controls.Button)(this.FindName("CancelRenButton")));
            this.ExistsPopUp = ((System.Windows.Controls.Primitives.Popup)(this.FindName("ExistsPopUp")));
            this.ExistPopupLay = ((System.Windows.Controls.Grid)(this.FindName("ExistPopupLay")));
            this.ExistsText = ((System.Windows.Controls.TextBlock)(this.FindName("ExistsText")));
            this.OkExistsButton = ((System.Windows.Controls.Button)(this.FindName("OkExistsButton")));
            this.CancelExistsButton = ((System.Windows.Controls.Button)(this.FindName("CancelExistsButton")));
            this.FileBox = ((System.Windows.Controls.ListBox)(this.FindName("FileBox")));
            this.CancelAllButton = ((System.Windows.Controls.Button)(this.FindName("CancelAllButton")));
            this.ProgressBox = ((System.Windows.Controls.ListBox)(this.FindName("ProgressBox")));
            this.DownProgressBox = ((System.Windows.Controls.ListBox)(this.FindName("DownProgressBox")));
            this.ASortingButton = ((System.Windows.Controls.Button)(this.FindName("ASortingButton")));
            this.SSortingButton = ((System.Windows.Controls.Button)(this.FindName("SSortingButton")));
            this.CopySelectedButton = ((System.Windows.Controls.Button)(this.FindName("CopySelectedButton")));
            this.PasteSelectedButton = ((System.Windows.Controls.Button)(this.FindName("PasteSelectedButton")));
            this.CheckAllButton = ((System.Windows.Controls.Button)(this.FindName("CheckAllButton")));
            this.PauseButton = ((System.Windows.Controls.Button)(this.FindName("PauseButton")));
            this.CreateNewFolderButton = ((System.Windows.Controls.Button)(this.FindName("CreateNewFolderButton")));
            this.RefreshButton = ((System.Windows.Controls.Button)(this.FindName("RefreshButton")));
        }
    }
}

