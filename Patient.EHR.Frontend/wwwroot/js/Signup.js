document.addEventListener("DOMContentLoaded", function () {

    const form = document.querySelector("form");
    const formError = document.getElementById("formError");
    const formErrorMessage = new URLSearchParams(window.location.search).get("error");

    if (formError && formErrorMessage) {
        formError.textContent = formErrorMessage;
        formError.hidden = false;
    }

    const password = document.getElementById("password");
    const confirmPassword = document.getElementById("confirmPassword");

    form.addEventListener("submit", function (e) {

        const pwd = password.value;

        const errorDiv =
            document.getElementById("passwordError");

        const confirmErrorDiv =
            document.getElementById("confirmPasswordError");

        errorDiv.style.display = "none";
        confirmErrorDiv.style.display = "none";

        const strongPassword =
            /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]).{8,}$/;

        if (!strongPassword.test(pwd)) {

            errorDiv.style.display = "block";

            errorDiv.innerText =
                "Password must contain at least 8 characters, one uppercase letter, one lowercase letter, one number and one special character.";

            e.preventDefault();
            return;
        }

        if (pwd !== confirmPassword.value) {

            confirmErrorDiv.style.display = "block";

            confirmErrorDiv.innerText =
                "Passwords do not match.";

            e.preventDefault();
            return;
        }

    });

});