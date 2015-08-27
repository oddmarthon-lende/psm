/// <copyright file="ireload.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>The reload interface</summary>
/// 

using System.Threading;
using System.Windows.Threading;

namespace PSMViewer.ViewModels
{
    /// <summary>
    /// Reload interface
    /// </summary>
    public interface IReload
    {

        Dispatcher Dispatcher { get; }
        CancellationTokenSource Cancel { get; }

        void Reload();

        bool Next();
        bool Previous();

    }

}
