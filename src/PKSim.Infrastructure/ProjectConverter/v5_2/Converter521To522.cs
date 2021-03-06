﻿using System.Xml.Linq;
using OSPSuite.Utility.Collections;
using OSPSuite.Utility.Visitor;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using PKSim.Core.Services;
using OSPSuite.Core.Domain;

namespace PKSim.Infrastructure.ProjectConverter.v5_2
{
   public class Converter521To522 : IObjectConverter,
      IVisitor<Simulation>,
      IVisitor<Individual>,
      IVisitor<RandomPopulation>,
      IVisitor<Compound>
   {
      private readonly IDefaultIndividualRetriever _defaultIndividualRetriever;
      private readonly ICloner _cloner;
      private readonly IRenalAgingCalculationMethodUpdater _renalAgingCalculationMethodUpdater;
      private readonly Cache<Species, Individual> _defaultCache;

      public Converter521To522(IDefaultIndividualRetriever defaultIndividualRetriever, ICloner cloner, IRenalAgingCalculationMethodUpdater renalAgingCalculationMethodUpdater)
      {
         _defaultIndividualRetriever = defaultIndividualRetriever;
         _cloner = cloner;
         _renalAgingCalculationMethodUpdater = renalAgingCalculationMethodUpdater;
         _defaultCache = new Cache<Species, Individual>(x => x.Species, x => null);
      }

      public bool IsSatisfiedBy(int version)
      {
         return version == ProjectVersions.V5_2_1;
      }

      public int Convert(object objectToConvert, int originalVersion)
      {
         this.Visit(objectToConvert);
         return ProjectVersions.V5_2_2;
      }

      public int ConvertXml(XElement element, int originalVersion)
      {
         //no xml to convert here
         return ProjectVersions.V5_2_2;
      }

      public void Visit(Simulation simulation)
      {
         var individual = simulation.Individual;
         Visit(individual);
         var compound = simulation.BuildingBlock<Compound>();
         Visit(compound);
         addIndividualParameters(simulation.Model.Root, individual.Species, simulation.Name);
         updateParametersOrigin(individual, simulation.Model.Root);
      }

      public void Visit(RandomPopulation population)
      {
         Visit(population.FirstIndividual);
      }

      public void Visit(Individual individual)
      {
         _renalAgingCalculationMethodUpdater.AddRenalAgingCalculationMethodTo(individual);
         addIndividualParameters(individual, individual.Species);
      }

      private void addIndividualParameters(IContainer individual, Species species, string replaceRootName = null)
      {
         if (!_defaultCache.Contains(species))
         {
            _defaultCache.Add(_defaultIndividualRetriever.DefaultIndividualFor(species));
         }

         var defaultIndividual = _defaultCache[species];

         var stomach = lumenStomachIn(individual);
         var defaultStomach = lumenStomachIn(defaultIndividual);

         stomach.Add(_cloner.Clone(defaultStomach.Parameter(ConverterConstants.Parameter.GET_Alpha_variability_factor)));
         stomach.Add(_cloner.Clone(defaultStomach.Parameter(ConverterConstants.Parameter.GET_Beta_variability_factor)));
      }

      private void updateParametersOrigin(Individual individual, IContainer root)
      {
         var indLargeIntestine = largeIntestineIn(individual);
         var simLargeIntestine = largeIntestineIn(root);

         updateParameterOrigin(indLargeIntestine, simLargeIntestine, ConverterConstants.Parameter.LITT_factor, individual.Id);
      }

      private void updateParameterOrigin(IContainer individualOrgan, IContainer simulationOrgan, string parameter, string individualId)
      {
         var indParameter = individualOrgan.Parameter(parameter);
         var simParameter = simulationOrgan.Parameter(parameter);
         simParameter.Origin.BuilingBlockId = individualId;
         simParameter.Origin.ParameterId = indParameter.Id;
      }

      private IContainer largeIntestineIn(IContainer container)
      {
         return container.Container(Constants.ORGANISM).Container(CoreConstants.Organ.LargeIntestine);
      }

      private IContainer lumenStomachIn(IContainer container)
      {
         return container.Container(Constants.ORGANISM)
            .Container(CoreConstants.Organ.Lumen)
            .Container(CoreConstants.Compartment.Stomach);
      }

      public void Visit(Compound compound)
      {
         //required for wrong conversion between 5.1 and 5.2.1
         var oldFractionUnbound = compound.Parameter(ConverterConstants.Parameter.FractionUnboundPlasma);
         if(oldFractionUnbound==null) return;

         oldFractionUnbound.Name = CoreConstants.Parameter.FractionUnbound;
      }
   }
}