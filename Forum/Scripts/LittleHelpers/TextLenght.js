$("textarea#area").keyup(function () {
    var charLength = $(this).val().length;
    var charLimit = 200;
    // Displays count
    $(this).next("#sArea").html(charLength + " of " + charLimit + " characters used");
    // Alert when max is reached
    if ($(this).val().length > charLimit) {
        $(this).next("#sArea").html("<strong>You may only have up to " + charLimit + " characters.</strong>");
    }
});