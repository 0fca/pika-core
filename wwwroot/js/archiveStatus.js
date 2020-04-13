const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/status",{
    transport: signalR.HttpTransportType.LongPolling | signalR.HttpTransportType.ServerSentEvents
})
    .configureLogging(signalR.LogLevel.Information)
    .build();



connection.on("ReceiveArchivingStatus", function (message) {
    reloadDownloadPartial(message);
});

connection.on("DownloadStarted", function () {
    hideDownloadBox();
});

connection.on("ArchivingCancelled", function (message) {
    reloadDownloadPartial(message);
});


connection.start().catch(function (err) {
    start();
    return console.error(err.toString());
});
