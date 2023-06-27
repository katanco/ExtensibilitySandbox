using Eto;

namespace ExtensibilitySandbox;

using Mendix.StudioPro.ExtensionsAPI.UI.DockablePane;
using System.ComponentModel.Composition;
using Eto.Forms;
using Eto.Drawing;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI;
using Eto.Forms.ThemedControls;
using System;

[Export(typeof(DockablePaneExtension))]
class TestPane : DockablePaneExtension
{

    // public DockablePanel() { }

    public override DockablePaneViewModel Open()
    {
        IProject CurrentProject = (IProject)CurrentApp.Root.Container;
        var Modules = CurrentProject.GetModules();

        var grid = new TreeGridView();
        var collection = new TreeGridItemCollection();

        foreach (var Module in Modules)
        {
            if (!Module.FromAppStore)
            {

                var ModuleItem = new TreeGridItem { Values = new object[] { true, Module.Name }, Expanded = true };
                foreach (var entity in Module.DomainModel.GetEntities())
                {
                    ModuleItem.Children.Add(new TreeGridItem { Values = new object[] { true, entity.Name }, Expanded = true });
                }
                collection.Add(ModuleItem);
            }
        }

        grid.DataStore = collection;



        // tree grid view

        // column 1
        var column1 = new GridColumn { HeaderText = "Selected", DataCell = new CheckBoxCell(0) };
        grid.Columns.Add(column1);

        // column 2
        var column2 = new GridColumn { HeaderText = "Name" };
        column2.DataCell = new TextBoxCell(1);
        grid.Columns.Add(column2);

        grid.CellClick += (s, e) =>
        {
            var TreeItem = (TreeGridItem)e.Item;
            var newState = !(bool)TreeItem.Values[0];
            if (TreeItem.Parent == null)
            {
                foreach (var Child in TreeItem.Children)
                {
                    var ChildTreeItem = (TreeGridItem)Child;
                    ChildTreeItem.SetValue(0, newState);
                }
            }
            else
            {
                if (newState == true)
                {
                    var ParentTreeItem = (TreeGridItem)TreeItem.Parent;
                    var SiblingsSelected = true;
                    foreach (var Child in ParentTreeItem.Children)
                    {
                        var ChildTreeItem = (TreeGridItem)Child;
                        if (!(bool)ChildTreeItem.Values[0] && ChildTreeItem != TreeItem)
                        {
                            SiblingsSelected = false;
                            break;
                        }
                    }
                    ParentTreeItem.SetValue(0, SiblingsSelected);
                }
                else
                {
                    var ParentTreeItem = (TreeGridItem)TreeItem.Parent;
                    ParentTreeItem.SetValue(0, newState);
                }
            }
            TreeItem.SetValue(0, newState);
            var TreeGrid = (TreeGridView)s;
            TreeGrid.ReloadData();
        };


        // layout
        var layout = new DynamicLayout
        {
            Padding = new Padding(10),
            Spacing = new Size(10, 10)
        };

        var Button = new Button();
        Button.Text = "Fire";
        Button.Click += (s, e) =>
        {
            var collection = (TreeGridItemCollection)grid.DataStore;
            var trueCollection = new List<TreeGridItem>();

            collection.AsEnumerable().ToList().ForEach(item =>
            {
                var TreeGridItem = (TreeGridItem)item; ;
                foreach (var Child in TreeGridItem.Children)
                {
                    var ChildTreeGridItem = (TreeGridItem)Child;
                    if ((bool)ChildTreeGridItem.Values[0])
                    {
                        trueCollection.Add(ChildTreeGridItem);

                        MessageBox.Show(ChildTreeGridItem.Values[1].ToString());
                    }
                }
            });

        };

        /*
        layout.Style = "DarkMode";

        Eto.Style.Add<TreeGridView>("DarkMode", widget =>
        {
            widget.BackgroundColor = Eto.Drawing.Colors.Black;
            widget.ShowHeader = false;
        });
        Eto.Style.Add<TextControl>("DarkMode", widget =>
        {
            widget.TextColor = Eto.Drawing.Colors.White;
        });
        */

        layout.BackgroundColor = Eto.Drawing.Colors.Gray;
        layout.Children.AsEnumerable().ToList().ForEach(item =>
        {

            MessageBox.Show(item.ToString());
        });

        layout.Add(grid);
        layout.Add(Button);





        return new DockablePaneViewModel
        {
            Title = "Productivity",
            Controls = {
                layout
            },
        };
    }

    public const string ID = "test-pane-1";
    public override string Id => ID;
}