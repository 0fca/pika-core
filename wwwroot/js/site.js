
function hideDownloadBox() {
    console.log("Hide download box.");
    let downloadPartial = document.getElementById("download-panel");
    downloadPartial.setAttribute("hidden", "true");
}

function reloadDownloadPartial(message) {
    let cancelArchivingButton = document.getElementById("cancelArchivingButton");
    cancelArchivingButton.setAttribute("disabled", "true");
    let info = document.getElementById("info");
    info.innerText = message;
    document.getElementById("progress-bar").removeAttribute("hidden");
}

function showMessagePartial(message, isError) {
    let ariaclass = " alert-" + (isError ? "danger" : "success");
    let alertText = document.getElementById("msg");
    let alertDiv = document.getElementById("alert");
    alertDiv.removeAttribute("hidden");
    alertDiv.getAttribute("class").concat(ariaclass);
    alertText.innerText = message;
}

function resetListOnView() {
    let uploadButton = document.getElementById("upload-submit");
    let filesList = document.getElementById("filesList");
    for (let listItem in fileList) {
        filesList.removeChild(listItem);
    }
    uploadButton.setAttribute("disabled", "true");
}

document.addEventListener('DOMContentLoaded', function () {
    M.AutoInit();
});

function getBrowser() {
    if (navigator.userAgent.indexOf("Chrome") !== -1) {
        return "Chrome";
    } else if (navigator.userAgent.indexOf("Opera") !== -1) {
        return "Opera";
    } else if (navigator.userAgent.indexOf("MSIE") !== -1) {
        return "IE";
    } else if (navigator.userAgent.indexOf("Firefox") !== -1) {
        return "Firefox";
    } else {
        return "unknown";
    }
}


