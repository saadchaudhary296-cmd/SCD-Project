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
    const email = document.getElementById("email");
    const fullName = document.getElementById("fullName");

    document.querySelectorAll("[data-toggle-password]").forEach((button) => {
        button.addEventListener("click", () => {
            const passwordField = document.getElementById(button.dataset.togglePassword);
            if (!passwordField) {
                return;
            }

            const isVisible = passwordField.type === "text";
            passwordField.type = isVisible ? "password" : "text";
            button.setAttribute("aria-pressed", String(!isVisible));
            button.setAttribute(
                "aria-label",
                `${isVisible ? "Show" : "Hide"} ${button.dataset.togglePassword === "password" ? "password" : "confirm password"}`
            );
        });
    });

    form.addEventListener("submit", function (e) {

        const name = fullName.value.trim();
        if (!/^[\p{L} ]+$/u.test(name)) {
            fullName.setCustomValidity("Use letters and spaces only for your name.");
            fullName.reportValidity();
            fullName.setCustomValidity("");
            e.preventDefault();
            return;
        }

        if (!email.value.trim().toLowerCase().endsWith("@gmail.com")) {
            email.setCustomValidity("Use a Gmail address ending in @gmail.com.");
            email.reportValidity();
            email.setCustomValidity("");
            e.preventDefault();
            return;
        }

        email.setCustomValidity("");

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