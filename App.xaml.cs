using Prism.Ioc;
using MultipleStartup.Views;
using System.Windows;
using System.Runtime.InteropServices;
using System;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

namespace MultipleStartup
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		// 外部プロセスのメイン・ウィンドウを起動するためのWin32 API
		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("user32.dll")]
		private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
		[DllImport("user32.dll")]
		private static extern bool IsIconic(IntPtr hWnd);
		// ShowWindowAsync関数のパラメータに渡す定義値(画面を元の大きさに戻す)
		private const int SW_RESTORE = 9;
		public Semaphore semaphore = null;

		protected override Window CreateShell()
		{
			// セマフォを使い多重起動のチェック
			semaphore = new Semaphore(1, 1, Assembly.GetExecutingAssembly().GetName().Name, out bool createdNew);
			// まだアプリが起動してなければ
			if (createdNew)
			{
				return Container.Resolve<MainWindow>();
			}
			// 既にアプリが起動していればそのアプリを前面に出す
			else
			{
				foreach (Process p in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
				{
					// 自分自身のプロセスIDは無視する
					if (p.Id != Environment.ProcessId)
					{
						// プロセスのフルパス名を比較して同じアプリケーションか検証
						if (p.MainModule.FileName == Process.GetCurrentProcess().MainModule.FileName)
						{
							// メイン・ウィンドウが最小化されていれば元に戻す
							if (IsIconic(p.MainWindowHandle))
							{
								_ = ShowWindowAsync(p.MainWindowHandle, SW_RESTORE);
							}
							// メイン・ウィンドウを最前面に表示する
							_ = SetForegroundWindow(p.MainWindowHandle);
						}
					}
				}
				Shutdown();
				return null;
			}
		}

		protected override void RegisterTypes(IContainerRegistry containerRegistry)
		{

		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			semaphore?.Dispose();
		}
	}
}
