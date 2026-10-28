// Tala's chat box: sends the question to /Tala/Ask and shows the answer as a chat bubble.
// All styling is Bootstrap classes, no custom CSS.

const form = document.getElementById("ask-form");
const input = document.getElementById("question");
const chat = document.getElementById("chat");

// Puts text in a bubble, one paragraph per line. textContent (not innerHTML) keeps any HTML in the text harmless.
function setText(bubble, text) {
    bubble.replaceChildren();
    for (const line of text.split("\n")) {
        const paragraph = document.createElement("p");
        paragraph.className = "mb-1";
        paragraph.textContent = line === "" ? "\u00A0" : line; // a non-breaking space keeps blank lines visible
        bubble.appendChild(paragraph);
    }
}

function addMessage(who, text) {
    document.getElementById("hello").classList.add("d-none");

    const bubble = document.createElement("div");
    bubble.className = who === "user"
        ? "align-self-end bg-success text-white rounded-3 px-3 py-2"
        : "align-self-start bg-white border rounded-3 px-3 py-2";
    setText(bubble, text);

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
        setText(bubble, data.answer);

        // Show which handbook sections the answer came from.
        if (data.sources && data.sources.length > 0) {
            const small = document.createElement("small");
            small.className = "d-block text-muted mt-2";
            small.textContent = "Sources: " + data.sources.join(" | ");
            bubble.appendChild(small);
        }
    } catch (error) {
        setText(bubble, "Sorry, something went wrong. Please try again.");
    }
});
