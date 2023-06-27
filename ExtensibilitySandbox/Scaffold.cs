using Eto;

namespace ExtensibilitySandbox;

using Mendix.StudioPro.ExtensionsAPI.UI.DockablePane;
using System.ComponentModel.Composition;
using Eto.Forms;
using Eto.Drawing;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using System;
using Mendix.StudioPro.ExtensionsAPI.Model.DomainModels;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Mendix.StudioPro.ExtensionsAPI.Model.Microflows;
using Mendix.StudioPro.ExtensionsAPI.Model.DataTypes;

[Export(typeof(DockablePaneExtension))]
class Scaffold : DockablePaneExtension
{
    private readonly IMicroflowService microflowService;
    private readonly INameValidationService nameValidationService;

    [ImportingConstructor]
    public Scaffold(IMicroflowService microflowService, INameValidationService nameValidationService)
    {
        this.microflowService = microflowService;
        this.nameValidationService = nameValidationService;
    }
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

                var ModuleItem = new TreeGridItem { Values = new object[] { true, Module.Name, Module }, Expanded = true };
                foreach (var entity in Module.DomainModel.GetEntities())
                {
                    ModuleItem.Children.Add(new TreeGridItem { Values = new object[] { true, entity.Name, entity }, Expanded = true });
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
        Button.Text = "Scaffold";
        Button.Click += (s, e) =>
        {
            var collection = (TreeGridItemCollection)grid.DataStore;

            collection.AsEnumerable().ToList().ForEach(item =>
            {
                var TreeGridItem = (TreeGridItem)item; ;
                foreach (var Child in TreeGridItem.Children)
                {
                    var ChildTreeGridItem = (TreeGridItem)Child;
                    if ((bool)ChildTreeGridItem.Values[0])
                    {
                        scaffoldEntity((IModule)TreeGridItem.Values[2], (IEntity)ChildTreeGridItem.Values[2]);
                    }
                }
            });

            

        };

        /*
         idk how to get dark mode to work

         grid.Style = "Dark";

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

        layout.Add(grid);
        layout.Add(Button);

        return new DockablePaneViewModel
        {
            Title = "Scaffold",
            Controls = {
                layout
            },
        };
    }

    public const string ID = "scaffold-pane-1";
    public override string Id => ID;

    private void scaffoldEntity(IModule module, IEntity entity)
    {
        if (CurrentApp == null)
        {
            throw new InvalidOperationException();
        }

        IProject CurrentProject = (IProject)CurrentApp.Root.Container;
        using ITransaction transaction = CurrentApp.StartTransaction("Scaffolding" + entity.Name);

        var objectsFolder = getCreateFolder(module, "Objects");
        var entityFolder = getCreateFolder(objectsFolder, entity.Name);

        var SaveMicroflow = CurrentApp.Create<IMicroflow>();
        SaveMicroflow.Name = entity.Name + "_Save";
        entityFolder.AddDocument(SaveMicroflow);

        /*
         * idk how to instantiate entitytype then
         * System.ArgumentOutOfRangeException: Mendix.StudioPro.ExtensionsAPI.Model.DataTypes.IEntityType is not a valid concrete element type. (Parameter 'T')
        at Mendix.Modeler.ExtensionLoader.ModelProxies.ModelProxy.Create[T]() in Mendix.Modeler.ExtensionLoader\ModelProxies\ModelProxy.cs:line 69

        var EntityType = CurrentApp.Create<IEntityType>();
        EntityType.Entity = entity.QualifiedName;
        

        SaveMicroflow.MicroflowReturnType = 
        microflowService.Initialize(SaveMicroflow, (entity.Name, null));
        */

        transaction.Commit();
        return;
    }

    private IFolder getCreateFolder(IFolderBase location, String name)
    {
        var FolderExists = location.GetFolders().Any(Folder => Folder.Name == name);
        if (FolderExists)
        {
            return location.GetFolders().First(Folder => Folder.Name == name);
        }
        var newObjectsFolder = CurrentApp.Create<IFolder>();
        newObjectsFolder.Name = name;
        location.AddFolder(newObjectsFolder);
        return newObjectsFolder;
    }
}