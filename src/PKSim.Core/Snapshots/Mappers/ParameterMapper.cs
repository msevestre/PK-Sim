﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Formulas;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Domain.UnitSystem;
using OSPSuite.Utility.Extensions;
using SnapshotParameter = PKSim.Core.Snapshots.Parameter;
using SnapshotTableFormula = PKSim.Core.Snapshots.TableFormula;
using ModelTableFormula = OSPSuite.Core.Domain.Formulas.TableFormula;

namespace PKSim.Core.Snapshots.Mappers
{
   public class ParameterMapper : ObjectBaseSnapshotMapperBase<IParameter, SnapshotParameter, IParameter>
   {
      private readonly TableFormulaMapper _tableFormulaMapper;
      private readonly IEntityPathResolver _entityPathResolver;

      public ParameterMapper(TableFormulaMapper tableFormulaMapper, IEntityPathResolver entityPathResolver)
      {
         _tableFormulaMapper = tableFormulaMapper;
         _entityPathResolver = entityPathResolver;
      }

      public override Task<SnapshotParameter> MapToSnapshot(IParameter modelParameter)
      {
         return createFrom<SnapshotParameter>(modelParameter, x => { x.Name = modelParameter.Name; });
      }

      public virtual async Task UpdateSnapshotFromParameter(SnapshotParameter snapshot, IParameter parameter)
      {
         snapshot.Value = parameter.ValueInDisplayUnit;
         snapshot.Unit = SnapshotValueFor(parameter.DisplayUnit.Name);
         snapshot.ValueDescription = SnapshotValueFor(parameter.ValueDescription);
         snapshot.TableFormula = await mapFormula(parameter.Formula);
      }

      private async Task<TSnapshotParameter> createFrom<TSnapshotParameter>(IParameter parameter, Action<TSnapshotParameter> configurationAction) where TSnapshotParameter : SnapshotParameter, new()
      {
         var snapshot = new TSnapshotParameter();
         await UpdateSnapshotFromParameter(snapshot, parameter);
         configurationAction(snapshot);
         return snapshot;
      }

      public override async Task<IParameter> MapToModel(SnapshotParameter snapshot, IParameter parameter)
      {
         parameter.ValueDescription = snapshot.ValueDescription;
         parameter.DisplayUnit = parameter.Dimension.Unit(ModelValueFor(snapshot.Unit));

         //only update formula if required
         if (snapshot.TableFormula != null)
            parameter.Formula = await _tableFormulaMapper.MapToModel(snapshot.TableFormula);

         if (snapshot.Value == null)
            return parameter;

         //This needs to come AFTER formula update so that the base value is accurate
         var baseValue = parameter.Value;
         var snapshotValueInBaseUnit = parameter.ConvertToBaseUnit(snapshot.Value);

         if (!ValueComparer.AreValuesEqual(baseValue, snapshotValueInBaseUnit))
            parameter.Value = snapshotValueInBaseUnit;

         return parameter;
      }

      private async Task<SnapshotTableFormula> mapFormula(IFormula formula)
      {
         if (!(formula is ModelTableFormula tableFormula))
            return null;

         return await _tableFormulaMapper.MapToSnapshot(tableFormula);
      }

      public virtual SnapshotParameter ParameterFrom(double? parameterBaseValue, string parameterDisplayUnit, IDimension dimension)
      {
         if (parameterBaseValue == null)
            return null;

         var displayUnitToUse = parameterDisplayUnit ?? dimension.BaseUnit.Name;

         return new SnapshotParameter
         {
            Value = dimension.BaseUnitValueToUnitValue(dimension.Unit(displayUnitToUse), parameterBaseValue.Value),
            Unit = displayUnitToUse
         };
      }

      public virtual Task<LocalizedParameter> LocalizedParameterFrom(IParameter parameter)
      {
         return LocalizedParameterFrom(parameter, _entityPathResolver.PathFor);
      }

      public virtual Task<LocalizedParameter> LocalizedParameterFrom(IParameter parameter, Func<IParameter, string> pathResolverFunc)
      {
         return createFrom<LocalizedParameter>(parameter, x => { x.Path = pathResolverFunc(parameter); });
      }

      public virtual Task<LocalizedParameter[]> LocalizedParametersFrom(IEnumerable<IParameter> parameters) => orderByPath(SnapshotMapperBaseExtensions.MapTo(parameters, LocalizedParameterFrom));

      public virtual Task<LocalizedParameter[]> LocalizedParametersFrom(IEnumerable<IParameter> parameters, Func<IParameter, string> pathResolverFunc)
      {
         return orderByPath(SnapshotMapperBaseExtensions.MapTo(parameters, x => LocalizedParameterFrom(x, pathResolverFunc)));
      }

      private async Task<LocalizedParameter[]> orderByPath(Task<LocalizedParameter[]> localizedParametersTask)
      {
         var localizedParameters = await localizedParametersTask;
         return localizedParameters?.OrderBy(x => x.Path).ToArray();
      }

      public virtual Task MapLocalizedParameters(IReadOnlyList<LocalizedParameter> localizedParameters, IContainer container)
      {
         if (localizedParameters == null || !localizedParameters.Any())
            return Task.FromResult(false);

         var allParameters = new PathCache<IParameter>(_entityPathResolver).For(container.GetAllChildren<IParameter>());
         var tasks = new List<Task>();
         localizedParameters.Each(snapshotParameter =>
         {
            var parameter = allParameters[snapshotParameter.Path];
            if (parameter == null)
               throw new SnapshotParameterNotFoundException(snapshotParameter.Path, container.Name);

            tasks.Add(MapToModel(snapshotParameter, parameter));
         });

         return Task.WhenAll(tasks);
      }
   }
}