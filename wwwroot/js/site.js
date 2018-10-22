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
}

function downloadResource(downloadPath) { 
    showDownloadBox();
    var w = window.open(downloadPath, '_blank', '', true);  
    console.log(w);
    w.onclose = function(){
        document.getElementById("download-panel").setAttribute("hidden",true);
    }
    //document.getElementById("download-panel").setAttribute("hidden", true);
}


function copyToClipboard(id) {
    let el = document.getElementById(id);
    el.select();
    document.execCommand('copy');
}

function onPathSpanHover() {
    let spanElement = document.getElementById("hoverSpan");
    let outputInput = document.getElementById("pathOutput");
    //console.log(isCtrlDown);

    if (isCtrlDown) {
        if (spanElement != null) {
            let childrenArr = spanElement.children;
            let resultPath = "";

            for (let i = 0; i < childrenArr.length; i++) {
                resultPath += childrenArr.item(i).textContent
            }
            outputInput.setAttribute("value", window.location.href + "?path=" + resultPath);
            outputInput.focus();
            outputInput.removeAttribute("hidden");
        }
    }
}

function onPathSpanOut() {
    let promptLbl = document.getElementById("pathOutput");
    promptLbl.setAttribute("hidden", true);
}

function scrollLogAreaToEnd() {
    var textarea = document.getElementById('log-area');
    textarea.scrollTop = textarea.scrollHeight;
}


