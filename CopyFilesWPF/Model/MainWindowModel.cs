﻿using System.Threading;
using System.Windows.Controls;
using static CopyFilesWPF.Model.FileCopier;

namespace CopyFilesWPF.Model
{
    public class MainWindowModel
    {
        public FilePath FilePath { get; set; } = new();

        public void CopyFile(ProgressChangeDelegate onProgressChanged, CompleteDelegate onComplete, Grid gridPanel)
        {
            var copier = new FileCopier(FilePath, onProgressChanged, onComplete, gridPanel);
            gridPanel.Tag = copier;
            
            var token = new CancellationTokenSource().Token;
            var newCopierThread = new Thread(() => copier.CopyFile(token))
            {
                IsBackground = true
            };
            newCopierThread.Start();
        }
    }
}
