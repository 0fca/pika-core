msgConnection.start().catch((err) => {
    return console.error(err.toString());
});

msgConnection.on("ReceiveReturnMessage",  (message, isError) =>{
    showMessagePartial(message, isError);
});
