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

const forgotPasswordDialog = document.getElementById("forgotPasswordDialog");
const forgotPasswordForm = document.getElementById("forgotPasswordForm");
const forgotPasswordMessage = document.getElementById("forgotPasswordMessage");
const resetEmail = document.getElementById("resetEmail");
const sendResetLinkButton = document.getElementById("sendResetLink");
const closeForgotPasswordButton = document.getElementById("closeForgotPassword");

if (forgotPasswordDialog && forgotPasswordForm && forgotPasswordMessage &&
    resetEmail && sendResetLinkButton && closeForgotPasswordButton) {
    document.querySelectorAll("[data-forgot-password]").forEach((link) => {
        link.addEventListener("click", (event) => {
            event.preventDefault();
            forgotPasswordMessage.textContent = "";
            resetEmail.value = document.getElementById("email")?.value ?? "";
            forgotPasswordDialog.showModal();
            resetEmail.focus();
        });
    });

    closeForgotPasswordButton.addEventListener("click", () => forgotPasswordDialog.close());

    forgotPasswordForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        forgotPasswordMessage.textContent = "";
        sendResetLinkButton.disabled = true;
        sendResetLinkButton.textContent = "Sending...";

        try {
            const response = await fetch("/Account/ForgotPassword", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email: resetEmail.value })
            });
            const result = await response.json();
            forgotPasswordMessage.textContent = result.message;
            forgotPasswordMessage.classList.toggle("is-error", !response.ok);
        } catch {
            forgotPasswordMessage.textContent = "We couldn't send the reset link. Please try again.";
            forgotPasswordMessage.classList.add("is-error");
        } finally {
            sendResetLinkButton.disabled = false;
            sendResetLinkButton.textContent = "Send reset link";
        }
    });
}

const resetPasswordForm = document.getElementById("resetPasswordForm");

if (resetPasswordForm) {
    const resetToken = new URLSearchParams(window.location.search).get("token");
    const resetPasswordMessage = document.getElementById("resetPasswordMessage");
    const resetPasswordButton = document.getElementById("resetPasswordButton");

    if (!resetToken) {
        resetPasswordMessage.textContent = "This reset link is invalid or has expired. Request a new one.";
        resetPasswordMessage.classList.add("is-error");
        resetPasswordButton.disabled = true;
    }

    resetPasswordForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        resetPasswordMessage.textContent = "";
        resetPasswordButton.disabled = true;
        resetPasswordButton.textContent = "Updating...";

        try {
            const response = await fetch("/Account/ResetPassword", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    token: resetToken,
                    password: document.getElementById("newPassword").value,
                    confirmPassword: document.getElementById("confirmNewPassword").value
                })
            });
            const result = await response.json();
            resetPasswordMessage.textContent = result.message;
            resetPasswordMessage.classList.toggle("is-error", !response.ok);
            if (response.ok) {
                resetPasswordForm.reset();
            }
        } catch {
            resetPasswordMessage.textContent = "We couldn't reset your password. Please try again.";
            resetPasswordMessage.classList.add("is-error");
        } finally {
            resetPasswordButton.disabled = false;
            resetPasswordButton.textContent = "Reset password";
        }
    });
}