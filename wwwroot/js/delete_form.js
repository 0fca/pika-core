document.querySelectorAll("span.secondary-content").forEach(el => {
    el.addEventListener('click', (e) => {
        const currentSpansIcon = document.getElementById(e.currentTarget.id);
        let iconText = currentSpansIcon.querySelector("i").innerText;
        if (iconText === "check") {
            currentSpansIcon.querySelector("i").innerText = "check_box_outline_blank";
        } else {
            currentSpansIcon.querySelector("i").innerText = "check";
        }
    });
});

document.getElementById("delete-button").addEventListener("click", async () => {
    try {
        let idListString = "";
        document.querySelectorAll("span[id]").forEach(el => {
            let iconText = el.querySelector("i").textContent;
            if (iconText === "check") {
                idListString += el.getAttribute("id") + ",";
            }
        });

        const response = await fetch("/Admin/RemoveMessagesExecute/" + idListString, {
            method: 'POST',
        });

        response.text()
            .then(function (text) {
                if (response.status === 202) {
                    window.location = response.headers.get("Location");
                }

                if (response.status === 400
                    || response.status === 500
                    || response.status === 200) {
                    document.querySelector("#output").setAttribute("class", "card-panel red white-text");
                    document.querySelector("#output").innerText = "Couldn't delete, bad request.";
                }
            });
    } catch (e) {
        document.querySelector("#output").innerText = e;
        document.querySelector("#output").setAttribute("class", "card-panel red white-text");
    }
});