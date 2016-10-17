/// <copyright file="ireload.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>The reload interface</summary>
/// 

using System.Threading;
using System.Windows.Threading;

namespace PSM.Viewer.Models
{

    /// <summary>
    /// Defines loading status
    /// </summary>
    public enum ReloadStatus
    {
        Unknown,
        Loading,
        Error,
        Idle
    }

    /// <summary>
    /// Reload interface
    /// </summary>
    public interface IReload
    {

        Dispatcher Dispatcher { get; }
        CancellationTokenSource CancellationTokenSource { get; set; }

        ReloadStatus Status { get; set; }

        void Reload();

        bool Next();
        bool Previous();

    }

}
