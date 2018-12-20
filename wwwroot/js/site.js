//var fileList = [];
var isCtrlDown = false;

function searchFileList(){
    let searchParam = document.getElementById("searchBox").value;
    let fileList = document.getElementById("file-list").children;

    if(searchParam != ""){
        for(let i = 0; i < fileList.length; i+=2){
            console.log(fileList[i].children[0]);
            if (!fileList[i].children[0].textContent.toLowerCase().includes(searchParam.toLowerCase())) {
                fileList[i].setAttribute("hidden", true);
                fileList[i].nextElementSibling.setAttribute("hidden", true);
            }else if(fileList[i].hasAttribute("hidden")){
                fileList[i].removeAttribute("hidden");
                fileList[i].nextElementSibling.removeAttribute("hidden");
            }
        }
    }else{
        resetFileList();
    }
}


function resetFileList(){
    var fileList = document.getElementById("file-list").children;
    for(let i = 0; i < fileList.length; i++){
        fileList[i].removeAttribute("hidden", false);  
    }
    document.getElementById("resetButton").setAttribute("disabled",true);
    document.getElementById("searchButton").removeAttribute("disabled");
}

function showDownloadBox(){
    document.getElementById("download-panel").removeAttribute("hidden");
    document.getElementById("cancelArchivingButton").removeAttribute("disabled");
} 

function hideDownloadBox() {
    let downloadPartial = document.getElementById("download-panel");
    downloadPartial.setAttribute("hidden", true);
}

function reloadDownloadPartial(message) {
    let cancelArchivingButton = document.getElementById("cancelArchivingButton");
    cancelArchivingButton.setAttribute("disabled", true);
    let info = document.getElementById("info");
    info.innerText = message;
    document.getElementById("progress-bar").removeAttribute("hidden");
}

function showMessagePartial(message, isError) {
    console.log(message);
    let ariaclass = " alert-" + (isError ? "danger" : "success");
    let alertText = document.getElementById("msg");
    let alertDiv = document.getElementById("alert");
    alertDiv.removeAttribute("hidden");
    alertDiv.getAttribute("class").concat(ariaclass);
    alertText.innerText = message;
}

function copyToClipboard(id) {
    let el = document.getElementById(id);
    el.select();
    document.execCommand('copy');
}

function onPathSpanOut() {
    let promptLbl = document.getElementById("pathOutput");
    promptLbl.setAttribute("hidden", true);
}

function scrollLogAreaToEnd() {
    var textarea = document.getElementById('log-area');
    textarea.scrollTop = textarea.scrollHeight;
}

function changeVisibleTab(controlName) {
    let contentId = controlName.textContent.toLowerCase().replace(" ", "-");
    //controlName.getAttribute("class").concat(" active");
    let targetDiv = document.getElementById("container");
    if (targetDiv.children.length > 0) {
        for (let childIndex = 0; childIndex < targetDiv.children.length; childIndex++) {
            if (targetDiv.children[childIndex].getAttribute("id") != contentId) {
                targetDiv.children[childIndex].setAttribute("hidden", true);
            } else {
                targetDiv.children[childIndex].removeAttribute("hidden");
            }
        }
    }
    if (contentId == "logs") {
        scrollLogAreaToEnd();
    }
}

function hideDownloadPartial() {
    let downloadPartial = document.getElementById("downloadPartialDiv");
    downloadPartial.setAttribute("hidden",true);
}

function resetListOnView() {
    let uploadButton = document.getElementById("upload-submit");
    let filesList = document.getElementById("filesList");
    for (let listItem in fileList) {
        filesList.removeChild(listItem);
    }
    uploadButton.setAttribute("disabled", true);
}


document.addEventListener('DOMContentLoaded', function () {
    var elems = document.querySelectorAll('.sidenav');
    M.AutoInit();
});

