// Signature whiteboard: black pen on a white canvas. The drawing is sent with the form as a PNG.

const board = document.getElementById("board");

if (board) {
    const context = board.getContext("2d");
    const hidden = document.getElementById("signatureData");
    const clearButton = document.getElementById("clear-board");

    let drawing = false;
    let hasInk = false;

    // Lets a finger draw on a phone without scrolling the page (a JS setting, not a stylesheet).
    board.style.touchAction = "none";

    // White background and black pen. The white is painted in so the saved PNG is not transparent.
    function resetBoard() {
        context.fillStyle = "#ffffff";
        context.fillRect(0, 0, board.width, board.height);
        context.strokeStyle = "#000000";
        context.lineWidth = 3;
        context.lineCap = "round";
        context.lineJoin = "round";
        hasInk = false;
        hidden.value = "";
    }

    // The canvas is shown scaled to the page width, so convert the pointer position to canvas pixels.
    function pointFrom(event) {
        const box = board.getBoundingClientRect();
        return {
            x: (event.clientX - box.left) * (board.width / box.width),
            y: (event.clientY - box.top) * (board.height / box.height)
        };
    }

    board.addEventListener("pointerdown", (event) => {
        drawing = true;
        board.setPointerCapture(event.pointerId);
        const point = pointFrom(event);
        context.beginPath();
        context.moveTo(point.x, point.y);
        context.lineTo(point.x + 0.1, point.y); // makes a single tap leave a dot
        context.stroke();
        hasInk = true;
    });

    board.addEventListener("pointermove", (event) => {
        if (!drawing) return;
        const point = pointFrom(event);
        context.lineTo(point.x, point.y);
        context.stroke();
    });

    board.addEventListener("pointerup", () => { drawing = false; });
    board.addEventListener("pointercancel", () => { drawing = false; });

    clearButton.addEventListener("click", resetBoard);

    // Copy the drawing into the hidden field right before the form is sent (empty if nothing was drawn).
    board.closest("form").addEventListener("submit", () => {
        hidden.value = hasInk ? board.toDataURL("image/png") : "";
    });

    resetBoard();
}
