﻿using System.Collections.Generic;
using System.Linq;
using OSPSuite.Utility.Extensions;
using PKSim.Core.Model;
using SnapshotTableFormula = PKSim.Core.Snapshots.DistributedTableFormula;
using ModelTableFormula = PKSim.Core.Model.DistributedTableFormula;

namespace PKSim.Core.Snapshots.Mappers
{
   public class DistributedTableFormulaMapper : SnapshotMapperBase<ModelTableFormula, SnapshotTableFormula>
   {
      private readonly IFormulaFactory _formulaFactory;
      private readonly TableFormulaMapper _tableFormulaMapper;

      public DistributedTableFormulaMapper(TableFormulaMapper tableFormulaMapper, IFormulaFactory formulaFactory)
      {
         _tableFormulaMapper = tableFormulaMapper;
         _formulaFactory = formulaFactory;
      }

      public override SnapshotTableFormula MapToSnapshot(ModelTableFormula tableFormula)
      {
         return SnapshotFrom(tableFormula, snapshot =>
         {
            _tableFormulaMapper.UpdateSnapshotProperties(snapshot, tableFormula);
            snapshot.Percentile = tableFormula.Percentile;
            snapshot.DistributionMetaData = distributionMetaDataFrom(tableFormula.AllDistributionMetaData());
         });
      }

      private List<DistributionMetaData> distributionMetaDataFrom(IReadOnlyList<Model.DistributionMetaData> allDistributionMetaData)
      {
         return allDistributionMetaData.Select(x => new DistributionMetaData
         {
            Mean = x.Mean,
            Deviation = x.Deviation,
            Distribution = x.Distribution.Id
         }).ToList();
      }

      public override ModelTableFormula MapToModel(SnapshotTableFormula snapshot)
      {
         var tableFormula = _formulaFactory.CreateDistributedTableFormula();
         _tableFormulaMapper.UpdateModelProperties(tableFormula, snapshot);
         tableFormula.Percentile = snapshot.Percentile;
         snapshot.DistributionMetaData.Select(modelDistributionDataFrom).Each(tableFormula.AddDistributionMetaData);
         return tableFormula;
      }

      private Model.DistributionMetaData modelDistributionDataFrom(DistributionMetaData distributionMetaData)
      {
         return new Model.DistributionMetaData
         {
            Mean = distributionMetaData.Mean,
            Deviation = distributionMetaData.Deviation,
            Distribution = DistributionTypes.ById(distributionMetaData.Distribution)
         };
      }
   }
}