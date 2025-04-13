using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CopyFilesWPF.Model
{
    public class FileCopier
    {
        private readonly Grid _gridPanel;
        private readonly FilePath _filePath;

        public delegate void ProgressChangeDelegate(double progress, Grid gridPanel);
        public delegate void CompleteDelegate(Grid gridPanel);
        public event ProgressChangeDelegate OnProgressChanged;
        public event CompleteDelegate OnComplete;

        public bool CancelFlag = false;
        public ManualResetEvent PauseFlag = new(true);

        public FileCopier(
            FilePath filePath,
            ProgressChangeDelegate onProgressChange,
            CompleteDelegate onComplete,
            Grid gridPanel)
        {
            OnProgressChanged += onProgressChange;
            OnComplete += onComplete;
            _filePath = filePath;
            _gridPanel = gridPanel;
        }

        public void CopyFile(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 1024];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var source = new FileStream(_filePath.PathFrom, FileMode.Open, FileAccess.Read);
                    var fileLength = source.Length;
                    using var destination = new FileStream(_filePath.PathTo, FileMode.CreateNew, FileAccess.Write);
                    
                    long totalBytes = 0;
                    int currentBlockSize;
                        
                    while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        totalBytes += currentBlockSize;
                        var percentage = totalBytes * 100.0 / fileLength;
                        destination.Write(buffer, 0, currentBlockSize);
                        OnProgressChanged(percentage, _gridPanel);

                        if(cancellationToken.IsCancellationRequested)
                        {
                            File.Delete(_filePath.PathTo);
                            return;
                        }
                        
                        Thread.CurrentThread.Suspend();
                    }

                    break;
                }
                catch (IOException error)
                {
                    HandleIOException(cancellationToken, error);
                    break;
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "Error occured!", MessageBoxButton.OK, MessageBoxImage.Error); 
                    break;
                }
            }
            OnComplete(_gridPanel);
        }

        private bool HandleIOException(CancellationToken cancellationToken, IOException error)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                var result = MessageBox.Show(error.Message + " Replace?", "Replace?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    File.Delete(_filePath.PathTo);
                    return true;
                }
            }
            else
            {
                MessageBox.Show(error.Message + " Copying was canceled!", "Cancel", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                File.Delete(_filePath.PathTo);
            }

            return false;
        }
    }
}
