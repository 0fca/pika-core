//var fileList = [];

function searchFileList(){
    let searchParam = document.getElementById("searchBox").value;
    //globalList = document.getElementById("file-list").innerHTML;
    let fileList = document.getElementById("file-list").children;

    let result = [];

    for(let i = 0; i < fileList.length; i+=2){
        //console.log(fileList[i].children[0].textContent);
        if(!fileList[i].children[0].textContent.includes(searchParam)){
            //console.log(fileList[i]);
            fileList[i].setAttribute("hidden", true);
            fileList[i].nextElementSibling.setAttribute("hidden", true);

        }
    }

    document.getElementById("resetButton").removeAttribute("disabled");
    document.getElementById("searchButton").setAttribute("disabled",true);
}

function resetFileList(){
    //wipeList();
    //console.log(fileList);
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


