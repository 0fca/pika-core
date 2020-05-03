document.querySelectorAll("span.secondary-content").forEach(el => {
    el.addEventListener('click', (e) => {
        const currentSpansIcon = document.getElementById(e.currentTarget.id);
        let iconText = currentSpansIcon.querySelector("i").innerText;
        if(iconText === "check"){
            currentSpansIcon.querySelector("i").innerText = "check_box_outline_blank";
        }else{
            currentSpansIcon.querySelector("i").innerText = "check";
        }
    });
});

document.getElementById("delete-button").addEventListener("click", async () => {
    try {
        let idListString = "";
        document.querySelectorAll("span.secondary-content").forEach(el => {
            let iconText = el.querySelector("i").innerText;
            if(iconText === "check"){
                idListString += el.getAttribute("id")+",";
            }
        });

        const response = await fetch("/Admin/RemoveMessagesExecute/"+idListString, {
            method: 'POST'
        });

        response.text()
            .then(function(text) {
                document.querySelector("#output").innerText = text;
                if(response.status === 202){
                    window.location = response.headers.get("Location");
                    document.querySelector("#output").setAttribute("class", "card-panel teal white-text");
                }
                if(response.status === 400
                    || response.status === 500){
                    document.querySelector("#output").setAttribute("class", "card-panel red white-text");
                }
            });
    }catch(e){
        document.querySelector("#output").innerText = e;
        document.querySelector("#output").setAttribute("class", "card-panel red white-text");
    }
});