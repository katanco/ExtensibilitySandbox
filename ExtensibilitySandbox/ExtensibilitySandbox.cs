using Eto.Forms;
using Mendix.StudioPro.ExtensionsAPI.UI.Menu;
using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI.Model;


namespace Mendix.Extensibility.ExtensibilitySandbox;

[Export(typeof(MenuBarExtension))]
public class TestExtension : MenuBarExtension
{
    [ImportingConstructor]
    public TestExtension()
    {
        // constructor
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
    }
}