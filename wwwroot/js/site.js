// Write your JavaScript code.
var globalList ="";

function searchFileList(){
    let searchParam = document.getElementById("searchBox").value;
    globalList = document.getElementById("file-list").innerHTML;
    let fileList = document.getElementById("file-list").innerHTML.split("<hr>");
    let resultArray = "";
    let i = 0;

    fileList.forEach(item =>{
        if(item.includes(searchParam)){
            console.log(item);
            resultArray += item+"\n"; 
        }
        i++;
    });
    document.getElementById("file-list").innerHTML = resultArray;
    document.getElementById("resetButton").setAttribute("disabled",true);
}

function resetFileList(){
    document.getElementById("file-list").innerHTML = globalList;
    document.getElementById("resetButton").removeAttribute("disabled");
}