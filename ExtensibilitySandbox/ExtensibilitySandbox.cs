using Eto.Forms;
using Mendix.StudioPro.ExtensionsAPI.UI.Menu;
using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI.Model.DomainModels;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;
using Mendix.StudioPro.ExtensionsAPI.Model.Enumerations;
using Mendix.StudioPro.ExtensionsAPI.Model.Texts;

namespace Mendix.Extensibility.ExtensibilitySandbox;

[Export(typeof(MenuBarExtension))]
public class TestExtension : MenuBarExtension
{

    private readonly IDockingWindowService dockingWindowService;

    [ImportingConstructor]
    public TestExtension()
    {
        this.dockingWindowService = dockingWindowService;
    }

    public override IEnumerable<MenuViewModelBase> GetMenus()
    {

        yield return new MenuItemViewModel("Test extension", placeUnder: new[] { "app" }, placeAfter: "tools")
        {
            Action = () =>
            {
                MessageBox.Show("Hello world!");
            }

        };

        yield return new MenuItemViewModel("NUKE", placeUnder: new[] { "app" }, placeAfter: "tools")
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

        yield return new MenuItemViewModel("Entity from csv", placeUnder: new[] { "app" }, placeAfter: "tools")
        {
            Action = () =>
            {
                if (CurrentApp != null)
                {
                    var ofd = new Eto.Forms.OpenFileDialog
                    {
                        MultiSelect = false,
                        Title = "Open"
                    };

                    var result = ofd.ShowDialog(new Panel());

                    if (result == DialogResult.Ok)
                    {
                        using (var reader = new StreamReader(ofd.FileName))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                var attributes = line.Split(',');

                                using ITransaction transaction = CurrentApp.StartTransaction("Entity from sheet");
                                IProject CurrentProject = (IProject)CurrentApp.Root.Container;
                                foreach (var module in CurrentProject.GetModules())
                                {
                                    if (!module.FromAppStore)
                                    {
                                        // TODO: change to selection
                                        IEntity entity = CurrentApp.Create<IEntity>();
                                        entity.Name = "newEntity";
                                        foreach (var attribute in attributes)
                                        {
                                            IAttribute NewAttribute = CurrentApp.Create<IAttribute>();
                                            NewAttribute.Name = attribute;
                                            entity.AddAttribute(NewAttribute);
                                        }
                                        module.DomainModel.AddEntity(entity);
                                        break;
                                    }
                                }
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        };

        yield return new MenuItemViewModel("Enum from csv", placeUnder: new[] { "app" }, placeAfter: "tools")
        {
            Action = () =>
            {
                if (CurrentApp != null)
                {
                    var ofd = new Eto.Forms.OpenFileDialog
                    {
                        MultiSelect = false,
                        Title = "Open"
                    };

                    var result = ofd.ShowDialog(new Panel());

                    if (result == DialogResult.Ok)
                    {
                        using (var reader = new StreamReader(ofd.FileName))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                var cells = line.Split(',');

                                using ITransaction transaction = CurrentApp.StartTransaction("Entity from sheet");
                                IProject CurrentProject = (IProject)CurrentApp.Root.Container;
                                foreach (var module in CurrentProject.GetModules())
                                {
                                    if (!module.FromAppStore)
                                    {
                                        // TODO: change to selection
                                        IEnumeration Enum = CurrentApp.Create<IEnumeration>();
                                        Enum.Name = ofd.FileName;
                                        foreach (var value in cells)
                                        {
                                            IEnumerationValue EnumValue = CurrentApp.Create<IEnumerationValue>();
                                            EnumValue.Name = value;
                                            IText EnumCaption = CurrentApp.Create<IText>();

                                            EnumCaption.AddOrUpdateTranslation("en_US", value);
                                            EnumValue.Caption = EnumCaption;
                                            Enum.AddValue(EnumValue);
                                        }
                                        module.AddDocument(Enum);
                                        break;
                                    }
                                }
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
        };
    }
}