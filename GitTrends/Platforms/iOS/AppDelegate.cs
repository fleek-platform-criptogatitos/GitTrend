﻿using AsyncAwaitBestPractices;
using Foundation;
using GitTrends.Shared;
using UIKit;

namespace GitTrends;

[Register(nameof(AppDelegate))]
public class AppDelegate : MauiUIApplicationDelegate
{
	public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
	{
		if (!Uri.TryCreate(url.AbsoluteString, UriKind.Absolute, out var callbackUri))
		{
			return false;
		}

		HandleCallbackUri(callbackUri).SafeFireAndForget(onException);

		return true;

		static async Task HandleCallbackUri(Uri callbackUri)
		{
			await ViewControllerServices.CloseSFSafariViewController().ConfigureAwait(false);

			var gitHubAuthenticationService = IPlatformApplication.Current?.Services.GetRequiredService<GitHubAuthenticationService>() ?? throw new InvalidOperationException($"Could not retrieve {nameof(GitHubAuthenticationService)}");
			await gitHubAuthenticationService.AuthorizeSession(callbackUri, CancellationToken.None).ConfigureAwait(false);
		}

		static void onException(Exception e)
		{
			IPlatformApplication.Current?.Services.GetRequiredService<IAnalyticsService>().Report(e);
		}
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp(AppInfo.Current);

	Task HandleLocalNotification(in UILocalNotification notification)
	{
		var notificationService = IPlatformApplication.Current?.Services.GetRequiredService<NotificationService>() ?? throw new InvalidOperationException($"Could not retrieve {nameof(NotificationService)}");

		if (notification.AlertTitle is null || notification.AlertBody is null)
			return Task.CompletedTask;

		return notificationService.HandleNotification(notification.AlertTitle, notification.AlertBody, (int)notification.ApplicationIconBadgeNumber, CancellationToken.None);
	}
}