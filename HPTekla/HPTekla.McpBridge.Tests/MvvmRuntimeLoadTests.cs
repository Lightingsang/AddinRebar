using System;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xunit;

namespace HPTekla.McpBridge.Tests;

public sealed class MvvmRuntimeLoadTests
{
    private sealed class SampleViewModel : ObservableObject
    {
        private string _title = "";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public IRelayCommand TestCommand { get; }

        public SampleViewModel()
        {
            TestCommand = new RelayCommand(() => Title = "Clicked");
        }
    }

    [Fact]
    public void BasicMvvmUsage_WorksWithoutAsyncInterfaces()
    {
        var vm = new SampleViewModel();
        Assert.Equal("", vm.Title);
        vm.TestCommand.Execute(null);
        Assert.Equal("Clicked", vm.Title);
    }

    [Fact]
    public void MvvmAsyncRelayCommand_RequiresAsyncInterfacesOrTasks()
    {
        // AsyncRelayCommand uses IAsyncRelayCommand which may reference IAsyncDisposable or async interfaces
        var cmd = new AsyncRelayCommand(async () =>
        {
            await System.Threading.Tasks.Task.Yield();
        });

        Assert.NotNull(cmd);
        Assert.True(cmd.CanExecute(null));
    }
}
