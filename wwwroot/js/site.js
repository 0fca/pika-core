//var fileList = [];

function searchFileList(){
    let searchParam = document.getElementById("searchBox").value;
    let fileList = document.getElementById("file-list").children;

    if(searchParam != ""){
        for(let i = 0; i < fileList.length; i+=2){
            console.log(fileList[i].children[0]);
            if(!fileList[i].children[0].textContent.includes(searchParam)){
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
        console.log("Reaches here.");
    }
    //document.getElementById("download-panel").setAttribute("hidden", true);
}


function copyToClipboard(){
    const el = document.getElementById('urlOut');
    el.select();
    document.execCommand('copy');
}


