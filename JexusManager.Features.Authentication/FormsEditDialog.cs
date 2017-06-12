﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace JexusManager.Features.Authentication
{
    using System;
    using System.Diagnostics;
    using System.Reactive.Linq;
    using System.Windows.Forms;

    using Microsoft.Web.Management.Client.Win32;
    using System.Reactive.Disposables;
    public partial class FormsEditDialog : DialogForm
    {
        public FormsEditDialog(IServiceProvider serviceProvider, FormsItem existing)
            : base(serviceProvider)
        {
            InitializeComponent();

            txtURL.Text = existing.LoginUrl;
            txtTimeout.Text = TimeSpanExtensions.GetTotalMinutes(existing.Timeout).ToString();

            cbMode.SelectedIndex = (int)existing.Mode;
            txtName.Text = existing.Name;
            cbProtectedMode.SelectedIndex = (int)existing.ProtectedMode;
            cbSSL.Checked = existing.RequireSsl;
            cbExpire.Checked = existing.SlidinngExpiration;
            btnOK.Enabled = false;

            var container = new CompositeDisposable();
            FormClosed += (sender, args) => container.Dispose();

            container.Add(
                Observable.FromEventPattern<EventArgs>(btnOK, "Click")
                .ObserveOn(System.Threading.SynchronizationContext.Current)
                .Subscribe(evt =>
                {
                    if (string.IsNullOrWhiteSpace(txtURL.Text))
                    {
                        ShowMessage(
                            "You must specify a login page URL.",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1);
                        return;
                    }

                    int minutes;
                    if (!int.TryParse(txtTimeout.Text, out minutes))
                    {
                        ShowMessage(
                            "You must specify a valid, positive integer for the authentication time-out.",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(txtName.Text))
                    {
                        ShowMessage(
                            "You must specify a cookie name.",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1);
                        return;
                    }

                    existing.LoginUrl = txtURL.Text;
                    existing.Timeout = new TimeSpan(0, 0, minutes, 0);
                    existing.Mode = cbMode.SelectedIndex;
                    existing.Name = txtName.Text;
                    existing.ProtectedMode = cbProtectedMode.SelectedIndex;
                    existing.RequireSsl = cbSSL.Checked;
                    existing.SlidinngExpiration = cbExpire.Checked;
                    existing.Apply();
                    DialogResult = DialogResult.OK;
                }));

            container.Add(
                Observable.FromEventPattern<EventArgs>(cbSSL, "CheckedChanged")
                .Merge(Observable.FromEventPattern<EventArgs>(txtURL, "TextChanged"))
                .Merge(Observable.FromEventPattern<EventArgs>(cbMode, "SelectedIndexChanged"))
                .Sample(TimeSpan.FromSeconds(1))
                .ObserveOn(System.Threading.SynchronizationContext.Current)
                .Subscribe(evt =>
                {
                    btnOK.Enabled = true;
                }));
        }

        private void FormsEditDialogHelpButtonClicked(object sender, CancelEventArgs e)
        {
            Process.Start("http://go.microsoft.com/fwlink/?LinkId=210461#Forms");
        }
    }
}
