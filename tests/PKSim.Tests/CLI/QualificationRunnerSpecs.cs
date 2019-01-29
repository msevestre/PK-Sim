﻿using System;
using System.IO;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Domain;
using OSPSuite.Utility;
using PKSim.CLI.Core.RunOptions;
using PKSim.CLI.Core.Services;
using PKSim.Core.Model;
using PKSim.Core.Services;
using PKSim.Core.Snapshots.Services;
using PKSim.Presentation.Core;
using static PKSim.Assets.PKSimConstants.Error;
using ILogger = OSPSuite.Core.Services.ILogger;
using Individual = PKSim.Core.Snapshots.Individual;
using SnapshotProject = PKSim.Core.Snapshots.Project;

namespace PKSim.CLI
{
   public abstract class concern_for_QualificationRunner : ContextSpecificationAsync<QualificationRunner>
   {
      protected ISnapshotTask _snapshotTask;
      protected IJsonSerializer _jsonSerializer;
      protected IWorkspace _workspace;
      protected IWorkspacePersistor _workspacePersistor;
      protected IExportSimulationRunner _exportSimulationRunner;
      protected ILogger _logger;
      protected QualificationRunOptions _runOptions;
      protected QualifcationConfiguration _qualificationConfiguration;
      private Func<string, string> _oldCreateDirectory;
      protected string _createdDirectory;
      private Func<string, bool> _oldFileExists;
      private Func<string, bool> _oldDirectoryExists;
      private Action<string, bool> _oldDeleteDirectory;

      public override async Task GlobalContext()
      {
         await base.GlobalContext();
         _oldCreateDirectory = DirectoryHelper.CreateDirectory;
         _oldDirectoryExists = DirectoryHelper.DirectoryExists;
         _oldDeleteDirectory = DirectoryHelper.DeleteDirectory;
         _oldFileExists = FileHelper.FileExists;
         DirectoryHelper.CreateDirectory = s => _createdDirectory = s;
      }

      protected override Task Context()
      {
         _snapshotTask = A.Fake<ISnapshotTask>();
         _jsonSerializer = A.Fake<IJsonSerializer>();
         _workspace = A.Fake<IWorkspace>();
         _workspacePersistor = A.Fake<IWorkspacePersistor>();
         _exportSimulationRunner = A.Fake<IExportSimulationRunner>();
         _logger = A.Fake<ILogger>();

         sut = new QualificationRunner(_snapshotTask, _jsonSerializer, _workspace, _workspacePersistor, _exportSimulationRunner, _logger);

         _runOptions = new QualificationRunOptions();
         _qualificationConfiguration = new QualifcationConfiguration();
         return _completed;
      }

      protected override Task Because()
      {
         return sut.RunBatchAsync(_runOptions);
      }

      public override async Task GlobalCleanup()
      {
         await base.GlobalCleanup();
         DirectoryHelper.CreateDirectory = _oldCreateDirectory;
         FileHelper.FileExists = _oldFileExists;
         DirectoryHelper.DirectoryExists = _oldDirectoryExists;
         DirectoryHelper.DeleteDirectory = _oldDeleteDirectory;
      }
   }

   public class When_running_the_qualification_runner_with_an_invalid_configuration : concern_for_QualificationRunner
   {
      [Observation]
      public void should_log_the_error()
      {
         A.CallTo(() => _logger.AddToLog(UnableToLoadQualificationConfigurationFromOptions, LogLevel.Error, A<string>._)).MustHaveHappened();
      }
   }

   public abstract class concern_for_QualificationRunnerWithValidConfiguration : concern_for_QualificationRunner
   {
      protected SnapshotProject _projectSnapshot;
      protected PKSimProject _project;
      protected const string PROJECT_NAME = "toto";

      protected override async Task Context()
      {
         await base.Context();
         _runOptions.Configuration = "XXX";
         A.CallTo(() => _jsonSerializer.DeserializeFromString<QualifcationConfiguration>(_runOptions.Configuration)).Returns(_qualificationConfiguration);
         UpdateConfiguration();
         _projectSnapshot = new SnapshotProject();
         _project = new PKSimProject().WithName(PROJECT_NAME);
         A.CallTo(() => _snapshotTask.LoadSnapshotFromFile<SnapshotProject>(_qualificationConfiguration.SnapshotPath)).Returns(_projectSnapshot);
         A.CallTo(() => _snapshotTask.LoadProjectFromSnapshot(_projectSnapshot)).Returns(_project);
         FileHelper.FileExists = s => string.Equals(s, _qualificationConfiguration.SnapshotPath);
      }

      protected virtual void UpdateConfiguration()
      {
         _qualificationConfiguration.OutputFolder = "AnOutputFolder";
         _qualificationConfiguration.SnapshotPath = $"{PROJECT_NAME}.json";
      }
   }

   public class When_running_the_qualification_runner_with_a_valid_configuration_resulting_in_an_output_folder_not_defined : concern_for_QualificationRunnerWithValidConfiguration
   {
      protected override void UpdateConfiguration()
      {
         _qualificationConfiguration.OutputFolder = "";
      }

      [Observation]
      public void should_log_the_error()
      {
         A.CallTo(() => _logger.AddToLog(QualificationOutputFolderNotDefined, LogLevel.Error, A<string>._)).MustHaveHappened();
      }
   }

   public class When_running_the_qualification_runner_with_a_valid_configuration_for_a_snapshot_file_that_does_not_exist : concern_for_QualificationRunnerWithValidConfiguration
   {
      protected override async Task Context()
      {
         await base.Context();
         FileHelper.FileExists = s => false;
      }

      [Observation]
      public void should_log_the_error()
      {
         A.CallTo(() => _logger.AddToLog(CannotLoadSnapshotFromFile(_qualificationConfiguration.SnapshotPath), LogLevel.Error, A<string>._)).MustHaveHappened();
      }
   }

   public class When_running_the_qualification_runner_with_a_valid_configuration_for_a_valid_snapshot_file : concern_for_QualificationRunnerWithValidConfiguration
   {
      private string _expectedOutputPath;
      private string _deletedDirectory;
      private ExportRunOptions _exportOptions;

      protected override async Task Context()
      {
         await base.Context();
         _expectedOutputPath = Path.Combine(_qualificationConfiguration.OutputFolder, PROJECT_NAME);
         DirectoryHelper.DirectoryExists = s => string.Equals(s, _expectedOutputPath);
         DirectoryHelper.DeleteDirectory = (s, b) => _deletedDirectory = s;

         A.CallTo(() => _exportSimulationRunner.ExportSimulationsIn(_project, A<ExportRunOptions>._))
            .Invokes(x => _exportOptions = x.GetArgument<ExportRunOptions>(1));
      }

      [Observation]
      public void should_have_set_the_name_of_the_project_snapshot_to_the_name_of_the_file()
      {
         _projectSnapshot.Name.ShouldBeEqualTo(PROJECT_NAME);
      }

      [Observation]
      public void should_delete_the_project_output_folder_under_the_output_folder_if_available()
      {
         _deletedDirectory.ShouldBeEqualTo(_expectedOutputPath);
      }

      [Observation]
      public void should_create_the_output_directory_for_the_project()
      {
         _createdDirectory.ShouldBeEqualTo(_expectedOutputPath);
      }

      [Observation]
      public void should_load_the_project_from_snapshot_and_export_its_simulations_to_the_output_folder()
      {
         _exportOptions.OutputFolder.ShouldBeEqualTo(_expectedOutputPath);
         _exportOptions.ExportMode.ShouldBeEqualTo(SimulationExportMode.All);
      }
   }

   public class When_running_the_qualification_runner_with_a_valid_configuration_with_swapable_building_blocks_based_on_an_invalid_file : concern_for_QualificationRunnerWithValidConfiguration
   {
      private BuildingBlockSwap _buildingBlockSwap;

      protected override async Task Context()
      {
         await base.Context();
         _buildingBlockSwap = new BuildingBlockSwap
         {
            Name = "Ind",
            Type = PKSimBuildingBlockType.Individual,
            SnapshotPath = "RefSnapshotPathDoesNotExist"
         };

         A.CallTo(() => _snapshotTask.LoadSnapshotFromFile<SnapshotProject>(_buildingBlockSwap.SnapshotPath)).Returns((SnapshotProject) null);
         _qualificationConfiguration.BuildingBlocks = new[] {_buildingBlockSwap};
      }

      [Observation]
      public void should_log_the_error_that_the_snapshot_was_not_found()
      {
         A.CallTo(() => _logger.AddToLog(CannotLoadSnapshotFromFile(_buildingBlockSwap.SnapshotPath), LogLevel.Error, A<string>._)).MustHaveHappened();
      }
   }

   public class When_running_the_qualification_runner_with_a_valid_configuration_with_swapable_building_blocks_based_on_a_valid_file_referencing_an_invalid_building_block_in_ref_project : concern_for_QualificationRunnerWithValidConfiguration
   {
      private BuildingBlockSwap _buildingBlockSwap;
      private SnapshotProject _refSnapshotProject;

      protected override async Task Context()
      {
         await base.Context();
         _buildingBlockSwap = new BuildingBlockSwap
         {
            Name = "Ind",
            Type = PKSimBuildingBlockType.Individual,
            SnapshotPath = "RefSnapshotPath.json"
         };

         _qualificationConfiguration.BuildingBlocks = new[] {_buildingBlockSwap};
         _refSnapshotProject = new SnapshotProject();
         A.CallTo(() => _snapshotTask.LoadSnapshotFromFile<SnapshotProject>(_buildingBlockSwap.SnapshotPath)).Returns(_refSnapshotProject);
      }

      [Observation]
      public void should_log_the_error_that_the_snapshot_was_not_found()
      {
         A.CallTo(() => _logger.AddToLog(CannotFindBuildingBlockInSnapshot(PKSimBuildingBlockType.Individual.ToString(), _buildingBlockSwap.Name, _buildingBlockSwap.SnapshotPath), LogLevel.Error, A<string>._)).MustHaveHappened();
      }
   }

   public class When_running_the_qualification_runner_with_a_valid_configuration_with_swapable_building_blocks_based_on_a_valid_file_referencing_an_invalid_building_block_in_base_project : concern_for_QualificationRunnerWithValidConfiguration
   {
      private BuildingBlockSwap _buildingBlockSwap;
      private SnapshotProject _refSnapshotProject;
      private Individual _refIndividual;

      protected override async Task Context()
      {
         await base.Context();
         _buildingBlockSwap = new BuildingBlockSwap
         {
            Name = "Ind",
            Type = PKSimBuildingBlockType.Individual,
            SnapshotPath = "RefSnapshotPath"
         };

         _qualificationConfiguration.BuildingBlocks = new[] {_buildingBlockSwap};
         _refIndividual = new Individual().WithName(_buildingBlockSwap.Name);
         _refSnapshotProject = new SnapshotProject {Individuals = new[] {_refIndividual}};
         A.CallTo(() => _snapshotTask.LoadSnapshotFromFile<SnapshotProject>(_buildingBlockSwap.SnapshotPath)).Returns(_refSnapshotProject);
      }

      [Observation]
      public void should_log_the_error_that_the_snapshot_was_not_found_in_the_project()
      {
         A.CallTo(() => _logger.AddToLog(CannotFindBuildingBlockInSnapshot(PKSimBuildingBlockType.Individual.ToString(), _buildingBlockSwap.Name, PROJECT_NAME), LogLevel.Error, A<string>._)).MustHaveHappened();
      }
   }

   public class When_running_the_qualification_runner_with_a_valid_configuration_with_swapable_building_blocks_based_on_a_valid_file_referencing_a_valud_building_blocks : concern_for_QualificationRunnerWithValidConfiguration
   {
      private BuildingBlockSwap _buildingBlockSwap;
      private SnapshotProject _refSnapshotProject;
      private Individual _refIndividual;

      protected override async Task Context()
      {
         await base.Context();
         _buildingBlockSwap = new BuildingBlockSwap
         {
            Name = "Ind",
            Type = PKSimBuildingBlockType.Individual,
            SnapshotPath = "RefSnapshotPath"
         };

         _qualificationConfiguration.BuildingBlocks = new[] {_buildingBlockSwap};
         _originalIndividual = new Individual().WithName(_buildingBlockSwap.Name);
         _projectSnapshot.Individuals = new[] {_originalIndividual,};

         _refIndividual = new Individual().WithName(_buildingBlockSwap.Name);
         _refSnapshotProject = new SnapshotProject {Individuals = new[] {_refIndividual}};

         A.CallTo(() => _snapshotTask.LoadSnapshotFromFile<SnapshotProject>(_buildingBlockSwap.SnapshotPath)).Returns(_refSnapshotProject);
      }

      private Individual _originalIndividual;

      [Observation]
      public void should_swap_the_individual_and_use_the_referenced_individual()
      {
         _projectSnapshot.Individuals.ShouldNotContain(_originalIndividual);
         _projectSnapshot.Individuals.ShouldContain(_refIndividual);
      }
   }
}