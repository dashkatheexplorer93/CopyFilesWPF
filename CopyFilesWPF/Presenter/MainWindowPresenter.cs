using System;
using CopyFilesWPF.Model;
using CopyFilesWPF.View;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CopyFilesWPF.Presenter
{
    public class MainWindowPresenter : IMainWindowPresenter
    {
        private readonly IMainWindowView _mainWindowView;
        private readonly MainWindowModel _mainWindowModel;
        private const double PanelHeight = 60;

        public MainWindowPresenter(IMainWindowView mainWindowView) {
            _mainWindowView = mainWindowView;
            _mainWindowModel = new MainWindowModel();
        }

        public void ChooseFileFromButtonClick(string path)
        {
            _mainWindowModel.FilePath.PathFrom = path;
        }

        public void ChooseFileToButtonClick(string path)
        {
            _mainWindowModel.FilePath.PathTo = path;
        }
        
        public void CopyButtonClick()
        {
            UpdateFilePaths();
            ClearTextBoxes();
            AdjustWindowHeight();

            var newPanel = CreateFileCopyPanel();
            _mainWindowView.MainWindowView.MainPanel.Children.Add(newPanel);

            _mainWindowModel.CopyFile(ProgressChanged, ModelOnComplete, newPanel);
        }

      private static void PauseCancelClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (sender is not Button { Tag: Grid { Tag: FileCopier fileCopier } } button)
            {
                return;
            }

            button.IsEnabled = false;

            var actionHandler = GetActionHandler(button.Content.ToString());
            actionHandler?.Invoke(fileCopier);
        }

        private static Action<FileCopier>? GetActionHandler(string? action)
        {
            return action switch
            {
                "Cancel" => fileCopier => fileCopier.CancelFlag = true,
                "Pause" => fileCopier => fileCopier.PauseFlag.Reset(),
                "Resume" => fileCopier => fileCopier.PauseFlag.Set(),
                _ => null
            };
        }

        private void ModelOnComplete(Grid panel)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate ()
                {
                    _mainWindowView.MainWindowView.Height -= PanelHeight;
                    _mainWindowView.MainWindowView.MainPanel.Children.Remove(panel);
                    _mainWindowView.MainWindowView.CopyButton.IsEnabled = true;
                }
            );
        }
        
        private void ProgressChanged(double percentage, Grid panel)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate ()
                {
                    UpdateProgressBar(panel, percentage);
                    UpdateButtonStates(panel);
                }
            );
        }

        private static void UpdateProgressBar(Grid panel, double percentage)
        {
            foreach (var el in panel.Children)
            {
                if (el is ProgressBar bar)
                {
                    bar.Value = percentage;
                }
            }
        }

        private static void UpdateButtonStates(Grid panel)
        {
            foreach (var el in panel.Children)
            {
                if (el is Button button)
                {
                   UpdateButtonState(button);
                }
            }
        }

        private static void UpdateButtonState(Button button)
        {
            var content = button.Content.ToString();
            switch (content)
            {
                case "Resume" when !button.IsEnabled:
                    button.Content = "Pause";
                    button.IsEnabled = true;
                    break;
                case "Pause" when !button.IsEnabled:
                    button.Content = "Resume";
                    button.IsEnabled = true;
                    break;
            }
        }
        
        private void UpdateFilePaths() 
        { 
            _mainWindowModel.FilePath.PathFrom = _mainWindowView.MainWindowView.FromTextBox.Text; 
            _mainWindowModel.FilePath.PathTo = _mainWindowView.MainWindowView.ToTextBox.Text; 
        }
        
        private void ClearTextBoxes() 
        { 
            _mainWindowView.MainWindowView.FromTextBox.Text = string.Empty; 
            _mainWindowView.MainWindowView.ToTextBox.Text = string.Empty; 
        }
        
        private void AdjustWindowHeight() 
        {
            _mainWindowView.MainWindowView.Height += PanelHeight;
        }
        private Grid CreateFileCopyPanel() 
        {
            var newPanel = new Grid 
            {
                Height = PanelHeight 
            };
            ConfigureGridLayout(newPanel); 
            AddFileNameTextBlock(newPanel);
            AddProgressBar(newPanel); 
            AddPauseButton(newPanel); 
            AddCancelButton(newPanel); 
            
            DockPanel.SetDock(newPanel, Dock.Top);
            return newPanel; 
        }
        private static void ConfigureGridLayout(Grid panel) 
        { 
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) }); 
            panel.ColumnDefinitions.Add(new ColumnDefinition()); 
            panel.ColumnDefinitions.Add(new ColumnDefinition()); 
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); 
            panel.RowDefinitions.Add(new RowDefinition()); 
        } 
        
        private void AddFileNameTextBlock(Grid panel) 
        { 
            var nameFile = new TextBlock 
            { 
                Text = Path.GetFileName(_mainWindowModel.FilePath.PathFrom), 
                Margin = new Thickness(5, 0, 5, 0) 
            }; 
            Grid.SetRow(nameFile, 0); 
            Grid.SetColumn(nameFile, 0); 
            panel.Children.Add(nameFile); 
        } 
        
        private static void AddProgressBar(Grid panel)
        { 
            var progressBar = new ProgressBar 
            { 
                Margin = new Thickness(10, 10, 10, 10) 
            }; 
            Grid.SetRow(progressBar, 1); 
            panel.Children.Add(progressBar); 
        } 
        
        private static void AddPauseButton(Grid panel) 
        { 
            var pauseButton = CreateButton("Pause", panel); 
            pauseButton.Click += PauseCancelClick; 
            Grid.SetRow(pauseButton, 1); 
            Grid.SetColumn(pauseButton, 1); 
            panel.Children.Add(pauseButton); 
        } 
        
        private static void AddCancelButton(Grid panel) 
        { 
            var cancelButton = CreateButton("Cancel", panel); 
            cancelButton.Click += PauseCancelClick; 
            Grid.SetRow(cancelButton, 1); 
            Grid.SetColumn(cancelButton, 2); 
            panel.Children.Add(cancelButton); 
        } 
        
        private static Button CreateButton(string content, Grid panel) 
        { 
            return new Button 
            { 
                Content = content, 
                Margin = new Thickness(5), 
                Tag = panel 
            }; 
        }
    }
}
