// Tala's chat box: sends the question to /Tala/Ask and shows the answer as a chat bubble.

const form = document.getElementById("ask-form");
const input = document.getElementById("question");
const chat = document.getElementById("chat");

// Adds one bubble to the chat. textContent (not innerHTML) keeps any HTML in the text harmless.
function addMessage(who, text) {
    document.getElementById("hello").style.display = "none";
    const bubble = document.createElement("div");
    bubble.className = "msg " + who;
    bubble.textContent = text;
    chat.appendChild(bubble);
    bubble.scrollIntoView({ behavior: "smooth", block: "end" });
    return bubble;
}

form.addEventListener("submit", async (event) => {
    event.preventDefault();

    const question = input.value.trim();
    if (question === "") return;

    addMessage("user", question);
    input.value = "";
    const bubble = addMessage("tala", "Thinking...");

    try {
        // The anti-forgery token proves this request came from our own page.
        const token = form.querySelector("input[name='__RequestVerificationToken']").value;

        const response = await fetch(form.dataset.url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token
            },
            body: JSON.stringify({ question: question })
        });
        if (!response.ok) throw new Error("Bad response: " + response.status);

        const data = await response.json();
        bubble.textContent = data.answer;

        // Show which handbook sections the answer came from.
        if (data.sources && data.sources.length > 0) {
            const small = document.createElement("small");
            small.textContent = "Sources: " + data.sources.join(" | ");
            bubble.appendChild(small);
        }
    } catch (error) {
        bubble.textContent = "Sorry, something went wrong. Please try again.";
    }
});
