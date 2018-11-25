connection.start().catch(function (err) {
    return console.error(err.toString());
});

connection.on("ReceiveArchivingStatus", function () {
    reloadDownloadPartial();
});

connection.on("DownloadStarted", function () {
    hideDownloadBox();
});

