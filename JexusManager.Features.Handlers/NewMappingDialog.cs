﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JexusManager.Features.Handlers
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Windows.Forms;

    using JexusManager.Features.FastCgi;
    using JexusManager.Features.Modules;

    using Microsoft.Web.Management.Client;
    using Microsoft.Web.Management.Client.Win32;

    internal sealed partial class NewMappingDialog : DialogForm
    {
        private readonly HandlersFeature _feature;

        public NewMappingDialog(IServiceProvider serviceProvider, HandlersItem existing, HandlersFeature feature)
            : base(serviceProvider)
        {
            InitializeComponent();
            Text = existing == null ? "Add Module Mapping" : "Edit Module Mapping";
            txtName.ReadOnly = existing != null;
            _feature = feature;
            Item = existing ?? new HandlersItem(null);

            var modules = new ModulesFeature((Module)serviceProvider);
            modules.Load();
            foreach (var module in modules.Items.Where(module => !module.IsManaged).OrderBy(module => module.Name))
            {
                txtModule.Items.Add(module.Name);
            }

            if (existing != null)
            {
                txtExecutable.Text = Item.ScriptProcessor;
                txtModule.Text = Item.Modules;
                txtName.Text = Item.Name;
                txtPath.Text = Item.Path;
            }

            var container = new CompositeDisposable();
            FormClosed += (sender, args) => container.Dispose();

            container.Add(
                Observable.FromEventPattern<EventArgs>(btnOK, "Click")
                .ObserveOn(System.Threading.SynchronizationContext.Current)
                .Subscribe(evt =>
                {
                    Item.ScriptProcessor = txtExecutable.Text;
                    Item.Modules = txtModule.Text;
                    Item.Name = txtName.Text;
                    Item.Path = txtPath.Text;
                    if (!txtName.ReadOnly)
                    {
                        if (_feature.Items.Any(item => item.Match(Item)))
                        {
                            ShowMessage(
                                "A handler with this name already exists.",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                MessageBoxDefaultButton.Button1);
                            return;
                        }

                        if (txtModule.Text == "FastCgiModule")
                        {
                            var result = ShowMessage(
                                "Do you want to create a FastCGI application for this executable? Click \"Yes\" to add the entry to the FastCGI collection and to enable this executable to run as a FastCGI application.",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxIcon.Information,
                                MessageBoxDefaultButton.Button1);
                            if (result == DialogResult.Yes)
                            {
                                var fastCgi = new FastCgiFeature((Module)ServiceProvider);
                                if (fastCgi.Items.All(item => item.Path != txtExecutable.Text))
                                {
                                    fastCgi.AddItem(new FastCgiItem(null) { Path = txtExecutable.Text });
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(txtExecutable.Text) && !File.Exists(txtExecutable.Text))
                        {
                            ShowMessage(
                                "The specific executable does not exist on the server.",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                MessageBoxDefaultButton.Button1);
                            return;
                        }
                    }

                    DialogResult = DialogResult.OK;
                }));

            container.Add(
                Observable.FromEventPattern<EventArgs>(txtName, "TextChanged")
                .Merge(Observable.FromEventPattern<EventArgs>(txtExecutable, "TextChanged"))
                .Merge(Observable.FromEventPattern<EventArgs>(txtPath, "TextChanged"))
                .Sample(TimeSpan.FromSeconds(1))
                .ObserveOn(System.Threading.SynchronizationContext.Current)
                .Subscribe(evt =>
                {
                    btnOK.Enabled = !string.IsNullOrWhiteSpace(txtName.Text)
                                    && !string.IsNullOrWhiteSpace(txtModule.Text)
                                    && !string.IsNullOrWhiteSpace(txtPath.Text);
                }));

            container.Add(
                Observable.FromEventPattern<EventArgs>(btnRestrictions, "Click")
                .ObserveOn(System.Threading.SynchronizationContext.Current)
                .Subscribe(evt =>
                {
                    var dialog = new RestrictionsDialog(ServiceProvider, Item);
                    if (dialog.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }));

            container.Add(
                Observable.FromEventPattern<EventArgs>(btnBrowse, "Click")
                .ObserveOn(System.Threading.SynchronizationContext.Current)
                .Subscribe(evt =>
                {
                    DialogHelper.ShowFileDialog(txtExecutable, "|*.dll||*.exe");
                }));
        }

        public HandlersItem Item { get; set; }

        private void NewRestrictionDialogHelpButtonClicked(object sender, CancelEventArgs e)
        {
            Process.Start("http://go.microsoft.com/fwlink/?LinkId=210505#Add_Module");
        }
    }
}
