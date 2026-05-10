window.Toast = (() => {

    let toast;
    let timer;

    function createToast() {

        if (document.getElementById("app-toast"))
            return;

        toast = document.createElement("div");
        toast.id = "app-toast";

        toast.innerHTML = `
            <div class="toast-dot"></div>
            <span id="app-toast-message"></span>
        `;

        toast.className = "app-toast";

        document.body.appendChild(toast);
    }

    function show(message = "Done", duration = 2200) {

        createToast();

        const msg = document.getElementById("app-toast-message");

        msg.textContent = message;

        toast.classList.add("show");

        clearTimeout(timer);

        timer = setTimeout(() => {
            toast.classList.remove("show");
        }, duration);
    }

    return {
        show
    };

})();