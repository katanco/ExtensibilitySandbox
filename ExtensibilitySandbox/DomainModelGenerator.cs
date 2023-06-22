/*
using System;
using System.Text.Json;
using Mendix.Modeler.DataImporter.Model;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.Model.DomainModels;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Attribute = Mendix.Modeler.DataImporter.Model.Attribute;

namespace Mendix.Modeler.DataImporter;

public class DomainModelGenerator
{
    readonly List<IAssociation> _createdAssociations;

    readonly List<IEntity> _createdEntities;
    readonly IModel _currentApp;
    readonly IModule _module;

    readonly UniqueNamesFinder _uniqueNamesFinder;
    readonly ILogService log;

    public DomainModelGenerator(IModel currentApp, IModule module, ILogService log)
    {
        _currentApp = currentApp;
        _module = module;
        this.log = log;

        _uniqueNamesFinder = new UniqueNamesFinder(module.GetDocuments().Select(document => document.Name).ToList());

        _createdEntities = new List<IEntity>();
        _createdAssociations = new List<IAssociation>();
    }

    public List<EntityId> CreatedEntityIds
    {
        get
        {
            return _createdEntities.ConvertAll(e =>
                new EntityId(e.Name, e.DataStorageGuid, e.GetAttributes()
                    .Select(a => new AttributeId(a.Name, a.DataStorageGuid))
                ));
        }
    }

    public List<AssociationId> CreatedAssociationIds
    {
        get
        {
            return _createdAssociations.ConvertAll(e =>
                new AssociationId(e.Name, e.DataStorageGuid)
            );
        }
    }

    public void GenerateDomainModel(SpreadsheetData result)
    {
        using var transaction = _currentApp.StartTransaction("Import domain model from DataImporter");

        CreateEntities(result);
        CreateAssociations(result);

        transaction.Commit();
    }

    public void CreateEntities(SpreadsheetData result)
    {
        var arranger = new EntityArranger();
        var associationAttributes = ParseAssociationAttributes(result);

        foreach (var parsedEntity in result.Entities)
        {
            var entityCreated = CreateEntity(parsedEntity, _module, associationAttributes, result);
            arranger.RepositionEntity(entityCreated, _module.DomainModel.GetEntities().ToList());
        }
    }

    public void CreateAssociations(SpreadsheetData result)
    {
        foreach (var (parentEntity, childEntity, associationSource) in GetAssociationParentAndChild(result.Associations))
        {
            var association = parentEntity.AddAssociation(childEntity);
            // write back the (possibly uniqified) name to the source data that will be used to create the DB
            // Not doing this will cause errors when we have multiple associations with (initially) the same name
            associationSource.Name = association.Name;
            _createdAssociations.Add(association);
        }
    }

    IEntity CreateEntity(Entity parsedEntity, IModule module, List<Attribute> associationAttributes, SpreadsheetData result)
    {
        var newEntity = _currentApp.Create<IEntity>();

        newEntity.Name = _uniqueNamesFinder.GetUniqueName(parsedEntity.Name);
        UpdateJsonEntityNames(parsedEntity.Name, newEntity.Name, result);

        parsedEntity.Attributes.ToList().ForEach(parsedAttribute =>
        {
            if (parsedAttribute.Type == null)
                throw new InvalidDataException($"Missing type for attribute {parsedAttribute.Name} of entity {parsedEntity.Name}.");

            if (associationAttributes.Contains(parsedAttribute))
                return;

            var newAttribute = _currentApp.Create<IAttribute>();
            newAttribute.Name = parsedAttribute.Name;
            newAttribute.Type = parsedAttribute.Type.ToMendixType(_currentApp, module, _uniqueNamesFinder);

            newEntity.AddAttribute(newAttribute);
        });

        module.DomainModel.AddEntity(newEntity);
        _createdEntities.Add(newEntity);

        return newEntity;
    }

    List<Attribute> ParseAssociationAttributes(SpreadsheetData result)
    {
        var associationAttributes = new List<Attribute>();

        foreach (var association in result.Associations)
        {
            var entityName = association.Parent?.WorkSheetName;
            var attributeName = association.Parent?.Header;
            if (entityName is null || attributeName is null)
                continue;

            var entity = result.Entities.Single(e => e.Name == entityName);
            var attribute = entity.Attributes.ToList().Single(a => a.Name == attributeName);

            associationAttributes.Add(attribute);
        }

        return associationAttributes;
    }

    void UpdateJsonEntityNames(string oldName, string newName, SpreadsheetData result)
    {
        if (oldName != newName)
            log.Info($"Renaming entity from ${oldName} to ${newName}");

        result.Entities.Single(e => e.Name == oldName).Name = newName;

        result.Associations = result.Associations.Select(association =>
        {
            if (association.Parent?.WorkSheetName == oldName)
                association.Parent.WorkSheetName = newName;

            if (association.Child?.WorkSheetName == oldName)
                association.Child.WorkSheetName = newName;

            return association;
        }).ToList();
    }

    IEnumerable<(IEntity, IEntity, Association)> GetAssociationParentAndChild(IList<Association> associations)
    {
        var entitiesDictionary = _module.DomainModel.GetEntities().ToDictionary(key => key.Name, v => v);

        foreach (var parsedAssociation in associations)
        {
            if (!parsedAssociation.Enabled || parsedAssociation.Parent == null ||
                parsedAssociation.Child == null) continue;

            var parentEntityName = parsedAssociation.Parent.WorkSheetName;
            var childEntityName = parsedAssociation.Child.WorkSheetName;

            if (parentEntityName == null || childEntityName == null) continue;

            var parentEntity = entitiesDictionary[parentEntityName];
            var childEntity = entitiesDictionary[childEntityName];

            yield return (parentEntity, childEntity, parsedAssociation);
        }
    }
}
*/