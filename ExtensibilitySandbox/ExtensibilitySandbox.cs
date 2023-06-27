using Eto.Forms;
using Mendix.StudioPro.ExtensionsAPI.UI.Menu;
using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI.Model.DomainModels;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;
using Mendix.StudioPro.ExtensionsAPI.Model.Enumerations;
using Mendix.StudioPro.ExtensionsAPI.Model.Texts;
using Mendix.StudioPro.ExtensionsAPI.Services;
using ExtensibilitySandbox;
using Mendix.StudioPro.ExtensionsAPI.Model.Microflows;
using System.Diagnostics;

namespace Mendix.Extensibility.ExtensibilitySandbox;

[Export(typeof(MenuBarExtension))]
public class TestExtension : MenuBarExtension
{
    private readonly IDockingWindowService dockingWindowService;
    private readonly ISelectorDialogService selectorDialogService;
    private readonly IMicroflowService microflowService;
    private readonly INameValidationService nameValidationService;

    [ImportingConstructor]
    public TestExtension(IDockingWindowService dockingWindowService, ISelectorDialogService selectorDialogService, IMicroflowService microflowService, INameValidationService nameValidationService)
    {
        this.dockingWindowService = dockingWindowService;
        this.selectorDialogService = selectorDialogService;
        this.microflowService = microflowService;
        this.nameValidationService = nameValidationService;
    }

    public override IEnumerable<MenuViewModelBase> GetMenus()
    {
        yield return new SubMenuViewModel("Productivity", placeUnder: new[] { "app" }, placeAfter: "tools")
        {
        };

        yield return new MenuItemViewModel("Nuke :)", placeUnder: new[] { "app", "Productivity" })
        {
            Action = () =>
            {
                if (CurrentApp != null)
                {
                    using (ITransaction transaction = CurrentApp.StartTransaction("NUKE"))
                    {
                        IProject CurrentProject = (IProject)CurrentApp.Root.Container;
                        foreach (var module in CurrentProject.GetModules())
                        {
                            if (!module.FromAppStore)
                            {
                                foreach (var folder in module.GetFolders())
                                {
                                    module.RemoveFolder(folder);
                                }

                                foreach (var document in module.GetDocuments())
                                {
                                    module.RemoveDocument(document);
                                }

                                foreach (var entity in module.DomainModel.GetEntities())
                                {
                                    module.DomainModel.RemoveEntity(entity);
                                }
                            }
                        }
                        transaction.Commit();
                    }

                    MessageBox.Show("goodbye :)");
                }
            }
        };

        yield return new MenuItemViewModel("Scaffold", placeUnder: new[] { "app", "Productivity" })
        {
            Action = () =>
            {
                dockingWindowService.OpenPane(Scaffold.ID);
            }
        };

        yield return new MenuItemViewModel("CSV to Entity", placeUnder: new[] { "app", "Productivity" })
        {
            Action = async () =>
            {
                if (CurrentApp != null)
                {
                    var ofd = new Eto.Forms.OpenFileDialog
                    {
                        MultiSelect = false,
                        Title = "Open"
                    };

                    var result = ofd.ShowDialog(new Panel());

                    IProject CurrentProject = (IProject)CurrentApp.Root.Container;
                    var targetModule = CurrentProject.GetModules().First(module => !module.FromAppStore);

                    var entityOptions = new EntitySelectorDialogOptions(targetModule, null);
                    entityOptions.CreateElement = (IModule module) =>
                    {
                        using ITransaction transaction = CurrentApp.StartTransaction("New Enum");
                        var Entity = CurrentApp.Create<IEntity>();
                        Entity.Name = nameValidationService.GetValidName(Path.GetFileNameWithoutExtension(ofd.FileName));
                        module.DomainModel.AddEntity(Entity);
                        transaction.Commit();
                        return Entity;
                    };

                    var resultDocument = await selectorDialogService.SelectEntityAsync(entityOptions);

                    if (result == DialogResult.Ok)
                    {
                        using (var reader = new StreamReader(ofd.FileName))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                if (line == null)
                                {
                                    throw new Exception("no valid data");
                                }
                                var attributes = line.Split(',');

                                using ITransaction transaction = CurrentApp.StartTransaction("Entity from sheet");

                                IEntity Entity;

                                if (resultDocument.Selection != null)
                                {
                                    Entity = resultDocument.Selection;
                                }
                                else
                                {
                                    Entity = CurrentApp.Create<IEntity>();
                                    Entity.Name = nameValidationService.GetValidName(Path.GetFileNameWithoutExtension(ofd.FileName));
                                }

                                foreach (var attribute in attributes)
                                {
                                    IAttribute NewAttribute = CurrentApp.Create<IAttribute>();
                                    NewAttribute.Name = attribute;
                                    Entity.AddAttribute(NewAttribute);
                                }


                                if (resultDocument.Selection == null)
                                {
                                    targetModule.DomainModel.AddEntity(Entity);
                                }

                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        };

        yield return new MenuItemViewModel("CSV to Enum", placeUnder: new[] { "app", "Productivity" })
        {
            Action = async () =>
            {
                if (CurrentApp != null)
                {
                    var ofd = new Eto.Forms.OpenFileDialog
                    {
                        MultiSelect = false,
                        Title = "Open"
                    };


                    var result = ofd.ShowDialog(new Panel());

                    IProject CurrentProject = (IProject)CurrentApp.Root.Container;
                    var targetModule = CurrentProject.GetModules().First(module => !module.FromAppStore);

                    var documentOptions = new DocumentSelectorDialogOptions<IEnumeration>(targetModule, null);
                    documentOptions.CreateElement = (IFolderBase folder) =>
                    {

                        using ITransaction transaction = CurrentApp.StartTransaction("New Enum");
                        var Enum = CurrentApp.Create<IEnumeration>();
                        Enum.Name = nameValidationService.GetValidName(Path.GetFileNameWithoutExtension(ofd.FileName));
                        folder.AddDocument(Enum);
                        transaction.Commit();
                        return Enum;
                    };

                    var resultDocument = await selectorDialogService.SelectDocumentAsync(documentOptions);

                    if (result == DialogResult.Ok && !resultDocument.IsCanceled)
                    {
                        using (var reader = new StreamReader(ofd.FileName))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                if (line == null)
                                {
                                    throw new Exception("no valid data");
                                }
                                var cells = line.Split(',');

                                using ITransaction transaction = CurrentApp.StartTransaction("Entity from sheet");

                                IEnumeration Enum;

                                if (resultDocument.Selection != null)
                                {
                                    Enum = resultDocument.Selection;
                                }
                                else
                                {
                                    Enum = CurrentApp.Create<IEnumeration>();
                                    Enum.Name = nameValidationService.GetValidName(Path.GetFileNameWithoutExtension(ofd.FileName));
                                }

                                foreach (var value in cells)
                                {
                                    IEnumerationValue EnumValue = CurrentApp.Create<IEnumerationValue>();
                                    EnumValue.Name = value.Trim().Replace(' ', '_');
                                    IText EnumCaption = CurrentApp.Create<IText>();

                                    EnumCaption.AddOrUpdateTranslation("en_US", value);
                                    EnumValue.Caption = EnumCaption;
                                    Enum.AddValue(EnumValue);
                                }
                                if (resultDocument.Selection == null)
                                {
                                    targetModule.AddDocument(Enum);
                                }

                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        };

        yield return new MenuItemViewModel("Insert Activity", placeUnder: new[] { "app", "Productivity" }, shortcutKey: Keys.P)
        {
            Action = () =>
            {
                if (CurrentApp != null)
                {
                    IAbstractUnit unit;
                    var success = dockingWindowService.TryGetActiveEditor(CurrentApp, out unit);

                    if (success)
                    {
                        {
                            using ITransaction transaction = CurrentApp.StartTransaction("Create microflow activity");
                            var action = CurrentApp.Create<IActionActivity>();
                            var actionList = new List<IActivity>() { action };

                            microflowService.TryInsertAfterStart((IMicroflow)unit, action);

                            microflowService.TryInsertBeforeActivity(action, CurrentApp.Create<IActionActivity>());


                            // appears not to work

                            var list = microflowService.GetAllMicroflowActivities((IMicroflow)unit);

                            var createSuccess = microflowService.TryInsertBeforeActivity(list.First(), CurrentApp.Create<IActionActivity>());
                            if (!createSuccess)
                            {
                                MessageBox.Show("unsuccessful create");
                            }



                            transaction.Commit();
                        };

                        /*
                        var activites = microflowService.GetAllMicroflowActivities((IMicroflow)unit);
                        foreach (var activity in activites)
                        {
                            MessageBox.Show(activity.ToString());
                        }
                        
                        var targetActivity = activites.Last(activity => true);
                        microflowService.TryInsertBeforeActivity(targetActivity, action);     
                        */

                    }
                }
            }
        };
    }
}