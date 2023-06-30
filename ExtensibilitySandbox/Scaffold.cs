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
    private readonly IMicroflowExpressionService microflowExpressionService;
    private readonly INameValidationService nameValidationService;
    private readonly IPageGenerationService pageGenerationService;

    [ImportingConstructor]
    public Scaffold(IMicroflowService microflowService, INameValidationService nameValidationService, IMicroflowExpressionService microflowExpressionService, IPageGenerationService pageGenerationService)
    {
        this.microflowService = microflowService;
        this.nameValidationService = nameValidationService;
        this.microflowExpressionService = microflowExpressionService;
        this.pageGenerationService = pageGenerationService;
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

        var objectsFolder = getCreateFolder(CurrentApp, module, "Objects");
        var pagesFolder = getCreateFolder(CurrentApp, module, "Pages");
        var objectsEntityFolder = getCreateFolder(CurrentApp, objectsFolder, entity.Name);
        var pagesEntityFolder = getCreateFolder(CurrentApp, pagesFolder, entity.Name);

        if (entity.GetAttributes() != null)
        {
            var documents = pageGenerationService.GenerateOverviewPages(module, new List<IEntity> { entity });
            // clone documents into pages folder
            // TODO: move instead of cloning to optimize
            foreach (var document in documents)
            {
                var cloned = CurrentApp.Copy<IDocument>(document);
                pagesEntityFolder.AddDocument(cloned);
            }
            var genFolder = (IFolder)documents.First().Container;
            module.RemoveFolder(genFolder);
        }

        var ValMicroflow = createEntityParameterMicroflow(CurrentApp, objectsEntityFolder, "SUB_" + entity.Name + "_Val", entity);
        ValMicroflow.MicroflowReturnType = CurrentApp.Create<IBooleanType>();

        var SubSaveMicroflow = createEntityParameterMicroflow(CurrentApp, objectsEntityFolder, "SUB_" + entity.Name + "_Save", entity);

        var ActSaveMicroflow = createEntityParameterMicroflow(CurrentApp, pagesEntityFolder, "ACT_" + entity.Name + "_Save", entity);

        var ValMicroflowCallActivity = createMicroflowCallActivity(CurrentApp, ValMicroflow, true, "isValid", (entity.Name, "$" + entity.Name));
        var SaveMicroflowCallActivity = createMicroflowCallActivity(CurrentApp, SubSaveMicroflow, false, "", (entity.Name, "$" + entity.Name));

        microflowService.TryInsertAfterStart(ActSaveMicroflow, SaveMicroflowCallActivity, ValMicroflowCallActivity);

        transaction.Commit();
        return;
    }

    private IFolder getCreateFolder(IModel currentApp, IFolderBase location, String name)
    {
        var FolderExists = location.GetFolders().Any(Folder => Folder.Name == name);
        if (FolderExists)
        {
            return location.GetFolders().First(Folder => Folder.Name == name);
        }
        var newObjectsFolder = currentApp.Create<IFolder>();
        newObjectsFolder.Name = name;
        location.AddFolder(newObjectsFolder);
        return newObjectsFolder;
    }

    private IActionActivity createMicroflowCallActivity(IModel currentApp, IMicroflow calledMicroflow, Boolean useReturnVariable, String outputVariableName, params (string parameterName, string expression)[] parameters)
    {
        var microflowCallActivity = currentApp.Create<IActionActivity>();
        var microflowCallAction = currentApp.Create<IMicroflowCallAction>();
        microflowCallAction.MicroflowCall = currentApp.Create<IMicroflowCall>();
        microflowCallAction.MicroflowCall.Microflow = calledMicroflow!.QualifiedName;
        microflowCallActivity!.Action = microflowCallAction;

        foreach (var (parameterName, expression) in parameters)
        {
            var parameterInCalledMicroflow = microflowService.GetParameters(calledMicroflow).Single(p => p.Name == parameterName);
            var parameterMapping = currentApp.Create<IMicroflowCallParameterMapping>();
            parameterMapping.Argument = microflowExpressionService.CreateFromString(expression);
            parameterMapping.Parameter = parameterInCalledMicroflow.QualifiedName;
            microflowCallAction.MicroflowCall.AddParameterMapping(parameterMapping);
        }

        if (useReturnVariable)
        {
            microflowCallAction.UseReturnVariable = true;
            microflowCallAction.OutputVariableName = outputVariableName;
        }

        return microflowCallActivity;
    }

    private IMicroflow createEntityParameterMicroflow(IModel currentApp, IFolderBase location, String name, IEntity entity)
    {
        var Microflow = currentApp.Create<IMicroflow>();
        Microflow.Name = name;

        location.AddDocument(Microflow);

        var EntityType = currentApp.Create<IObjectType>();
        EntityType.Entity = entity.QualifiedName;
        microflowService.Initialize(Microflow, (entity.Name, EntityType));

        return Microflow;
    }
}