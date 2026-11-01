window.togglePassword = function () {
    const passwordField = document.getElementById("password");

    if (passwordField.type === "password") {
        passwordField.type = "text";
    } else {
        passwordField.type = "password";
    }
};

const loginError = document.getElementById("formError");
const loginErrorMessage = new URLSearchParams(window.location.search).get("error");

if (loginError && loginErrorMessage) {
    loginError.textContent = loginErrorMessage;
    loginError.hidden = false;
    loginError.style.display = "block";
}

const backButton = document.getElementById("backToLogin");

if (backButton) {
    backButton.addEventListener("click", function () {
        window.location.href = "/Account/Login";
    });
}