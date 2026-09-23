using DealerManager.Common.Attributes;
using DealerManager.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DealerManager.Infrastructure.Helpers
{
    public static class EntityHelper
    {
        public static void UpdateFromDto(IEntity original, object modificationDto, DbContext context, bool stateModified = true)
        {
            if (stateModified)
            {
                context.Entry(original).State = EntityState.Modified;
            }

            var dtoProperties = modificationDto.GetType().GetProperties(
                    BindingFlags.Public
                    | BindingFlags.Instance);

            foreach (var dtoItem in dtoProperties)
            {
                var item = original.GetType().GetProperty(dtoItem.Name);

                if (item == null || item.Name == "Id" || item.Name == "Version" || item.Name == "ViewOrder" || SkipAttribute.IsDeclared(item) || SkipUpdateAttribute.IsDeclared(item))
                {
                    continue;
                }

                if (item.CanWrite)
                {
                    var modificationDtoValue = dtoItem.GetValue(modificationDto, null);
                    var originalValue = item.GetValue(original, null);

                    // Value
                    if (item.PropertyType.IsValueType
                            || item.PropertyType.IsEnum
                            || item.PropertyType.Equals(typeof(string)))
                    {
                        item.SetValue(original, modificationDtoValue, null);
                    }
                    // Collection
                    else if (modificationDtoValue != null
                            && modificationDtoValue is IList)
                    {
                        var originalItems = ((IEnumerable)originalValue).Cast<IEntity>().ToList();
                        var newDtoItems = ((IEnumerable)modificationDtoValue).Cast<object>().ToList();

                        var addMethod = originalValue.GetType().GetMethods()
                            .Where(m => m.Name == "Add")
                            .FirstOrDefault();

                        var removeMethod = originalValue.GetType().GetMethods()
                            .Where(m => m.Name == "Remove")
                            .FirstOrDefault();

                        var forAddDtos = newDtoItems.Where(newItem =>
                            !originalItems.Any(originalItem => originalItem.Id == (int?)newItem.GetType().GetProperty("Id").GetValue(newItem, null))).ToList();

                        foreach (var forAddDto in forAddDtos)
                        {
                            var forAddItem = Activator.CreateInstance(originalValue.GetType().GetGenericArguments()[0]);
                            UpdateFromDto((IEntity)forAddItem, forAddDto, context, false);
                            context.Attach(forAddItem);
                            addMethod.Invoke(originalValue, [forAddItem]);
                        }

                        var forDelete = originalItems.Where(originalItem =>
                            !newDtoItems.Any(newItem => (int?)newItem.GetType().GetProperty("Id").GetValue(newItem, null) == originalItem.Id))
                            .ToList();

                        if (!SkipDeleteAttribute.IsDeclared(item))
                        {
                            foreach (var item2 in forDelete)
                            {
                                Remove(item2, context);
                                removeMethod.Invoke(originalValue, [item2]);
                            }
                        }

                        var forUpdate = originalItems.Where(originalItem =>
                            newDtoItems.Any(newItem => (int?)newItem.GetType().GetProperty("Id").GetValue(newItem, null) == originalItem.Id))
                            .ToList();

                        foreach (var item2 in forUpdate)
                        {
                            UpdateFromDto(item2, newDtoItems.Single(t => (int?)t.GetType().GetProperty("Id").GetValue(t, null) == item2.Id), context);
                        }
                    }
                    // Object
                    else
                    {
                        if (originalValue != null
                            && modificationDtoValue == null
                            && !SkipDeleteAttribute.IsDeclared(item))
                        {
                            Remove(originalValue, context);
                            item.SetValue(original, null);
                        }
                        else if (originalValue == null
                            && modificationDtoValue != null)
                        {
                            var forAddObject = Activator.CreateInstance(item.PropertyType);
                            UpdateFromDto((IEntity)forAddObject, modificationDtoValue, context, false);
                            context.Attach(forAddObject);
                            item.SetValue(original, forAddObject);
                        }
                        else if (originalValue != null && modificationDtoValue != null)
                        {
                            UpdateFromDto((IEntity)originalValue, modificationDtoValue, context);
                        }
                    }
                }
            }
        }

        public static void Update(IEntity original, IEntity modification, DbContext context)
        {
            context.Entry(original).State = EntityState.Modified;

            var properties = original.GetType().GetProperties(
                    BindingFlags.Public
                    | BindingFlags.Instance);

            foreach (var item in properties)
            {
                if (item.Name == "Id" || item.Name == "Version" || item.Name == "ViewOrder" || SkipAttribute.IsDeclared(item) || SkipUpdateAttribute.IsDeclared(item))
                {
                    continue;
                }

                if (item.CanWrite)
                {
                    var modificationValue = item.GetValue(modification, null);
                    var originalValue = item.GetValue(original, null);

                    // Value
                    if (item.PropertyType.IsValueType
                            || item.PropertyType.IsEnum
                            || item.PropertyType.Equals(typeof(string)))
                    {
                        item.SetValue(original, modificationValue, null);
                    }
                    // Collection
                    else if (modificationValue != null
                            && modificationValue is IList)
                    {
                        var originalItems = ((IEnumerable)originalValue).Cast<IEntity>().ToList();
                        var newItems = ((IEnumerable)modificationValue).Cast<IEntity>().ToList();

                        var addMethod = originalValue.GetType().GetMethods()
                            .Where(m => m.Name == "Add")
                            .FirstOrDefault();

                        var removeMethod = originalValue.GetType().GetMethods()
                            .Where(m => m.Name == "Remove")
                            .FirstOrDefault();

                        var forAdd = newItems.Where(newItem =>
                            !originalItems.Any(originalItem => originalItem.Id == newItem.Id)).ToList();

                        // If Id > 0 AttachRange is not adding. Use Attach when there are Nomenclatures in the List.
                        // Add is used in custom cases (When Id is set outside like InstitutionSpecialities from RND)
                        foreach (var forAddItem in forAdd)
                        {
                            var id = (int?)forAddItem.GetType().GetProperty("Id").GetValue(forAddItem, null);

                            if (id > 0)
                            {
                                var forAddItemProperties = forAddItem.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                foreach (var forAddItemProperty in forAddItemProperties)
                                {
                                    if (SkipAttribute.IsDeclared(forAddItemProperty))
                                    {
                                        forAddItemProperty.SetValue(forAddItem, null, null);
                                    }
                                }
                                context.Add(forAddItem);
                            }
                            else
                            {
                                context.Attach(forAddItem);
                            }
                        }

                        foreach (var item2 in forAdd)
                        {
                            addMethod.Invoke(originalValue, [item2]);
                        }

                        var forDelete = originalItems.Where(originalItem =>
                            !newItems.Any(newItem => newItem.Id == originalItem.Id))
                            .ToList();

                        if (!SkipDeleteAttribute.IsDeclared(item))
                        {
                            foreach (var item2 in forDelete)
                            {
                                Remove(item2, context);
                                removeMethod.Invoke(originalValue, new object[] { item2 });
                            }
                        }

                        var forUpdate = originalItems.Where(originalItem =>
                            newItems.Any(newItem => newItem.Id == originalItem.Id))
                            .ToList();

                        foreach (var item2 in forUpdate)
                        {
                            Update(item2, newItems.Single(t => t.Id == item2.Id), context);
                        }
                    }
                    // Object
                    else
                    {
                        if (originalValue != null
                            && modificationValue == null
                            && !SkipDeleteAttribute.IsDeclared(item))
                        {
                            Remove(originalValue, context);
                            item.SetValue(original, null);
                        }
                        else if (originalValue == null
                            && modificationValue != null)
                        {
                            context.Attach(modificationValue);
                            item.SetValue(original, modificationValue);
                        }
                        else if (originalValue != null && modificationValue != null)
                        {
                            Update((IEntity)originalValue, (IEntity)modificationValue, context);
                        }
                    }
                }
            }
        }

        public static void Remove<T>(T entity, DbContext context)
        {
            var properties = entity.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var item in properties)
            {
                if (item.PropertyType.IsValueType
                    || item.PropertyType.IsEnum
                    || item.PropertyType.Equals(typeof(string))
                    || SkipAttribute.IsDeclared(item))
                {
                    continue;
                }

                var value = item.GetValue(entity, null);
                if (value == null)
                {
                    continue;
                }

                if (value is IList list)
                {
                    foreach (var item2 in list)
                    {
                        Remove(item2, context);
                    }
                }
                else
                {
                    Remove(value, context);
                }
            }

            context.Remove(entity);
        }

        public static object CloneObject(object entity)
        {
            var newEntity = Activator.CreateInstance(entity.GetType());
            var properties = newEntity.GetType().GetProperties(
                    BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.NonPublic);

            foreach (var item in properties)
            {
                // Skip cloning some properties
                if (item.Name == "Id" || item.Name == "Version" || SkipAttribute.IsDeclared(item))
                {
                    continue;
                }

                var value = item.GetValue(entity, null);
                if (item.CanWrite && value != null)
                {
                    if (item.PropertyType.IsValueType
                            || item.PropertyType.IsEnum
                            || item.PropertyType.Equals(typeof(string)))
                    {
                        item.SetValue(newEntity, value, null);
                    }
                    else if (value.GetType().GetInterface("IList", true) != null)
                    {
                        // No need to create instance, if there is initialization in constructor.
                        var collection = item.GetValue(newEntity);

                        foreach (var item2 in (IEnumerable)value)
                        {
                            ((IList)collection).Add(CloneObject(item2));
                        }
                    }
                    else
                    {
                        if (value != null)
                        {
                            item.SetValue(newEntity, CloneObject(value), null);
                        }
                    }
                }
            }

            return newEntity;
        }

        public static void CloneProperties(object originalEntity, object updateEntity)
        {
            var originalProperties = originalEntity.GetType().GetProperties(
                    BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.NonPublic);

            foreach (var originalProperty in originalProperties)
            {
                if (originalProperty.Name == "Id" || originalProperty.Name == "Version" || SkipAttribute.IsDeclared(originalProperty))
                {
                    continue;
                }

                PropertyInfo updateProperty = updateEntity.GetType().GetProperty(originalProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                var value = originalProperty.GetValue(originalEntity, null);

                if (updateProperty != null && updateProperty.CanWrite && value != null)
                {
                    if (updateProperty.PropertyType.IsValueType
                            || updateProperty.PropertyType.IsEnum
                            || updateProperty.PropertyType.Equals(typeof(string)))
                    {
                        updateProperty.SetValue(updateEntity, value, null);
                    }
                    else if (value.GetType().GetInterface("IList", true) != null)
                    {
                        // No need to create instance, if there is initialization in constructor.
                        var updateCollection = updateProperty.GetValue(updateEntity);

                        foreach (var originalCollectionItem in (IEnumerable)value)
                        {
                            var updateCollectionItem = Activator.CreateInstance(updateProperty.PropertyType.GetGenericArguments()[0]);
                            CloneProperties(originalCollectionItem, updateCollectionItem);
                            ((IList)updateCollection).Add(updateCollectionItem);
                        }
                    }
                    else
                    {
                        if (updateProperty.PropertyType.IsClass && value != null)
                        {
                            var updateObject = updateProperty.GetValue(updateEntity);

                            if (updateObject == null)
                            {
                                updateObject = Activator.CreateInstance(updateProperty.PropertyType);
                            }

                            CloneProperties(value, updateObject);

                            updateProperty.SetValue(updateEntity, updateObject, null);
                        }
                    }
                }
            }
        }

        public static void ClearSkipProperties(object entity)
        {
            var properties = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.PropertyType.IsValueType
                        || property.PropertyType.IsEnum
                        || property.PropertyType.Equals(typeof(string)))
                {
                    continue;
                }

                if (SkipAttribute.IsDeclared(property))
                {
                    property.SetValue(entity, null);
                }
                else
                {
                    var propertyValue = property.GetValue(entity, null);
                    if (propertyValue == null)
                    {
                        continue;
                    }

                    if (propertyValue is IList)
                    {
                        foreach (var item in (IList)propertyValue)
                        {
                            ClearSkipProperties(item);
                        }
                    }
                    else
                    {
                        ClearSkipProperties(propertyValue);
                    }
                }
            }
        }
    }
}
