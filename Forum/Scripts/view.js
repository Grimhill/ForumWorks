$(document).ready(function () {
        //view/hide textarea for reply comment
    $(".commentExpCtrl").click(function () {
        var clicks = $(this).data('clicks');
        if (!clicks) {
            commentExpand(this, '.maincomment', '.userComment');
            commentExpand(this, '.mainreply', '.parentReply');
            commentExpand(this, '.childReplyCont', '.childReply');
        } else {
            commentCollapse(this, '.maincomment', '.userComment');
            commentCollapse(this, '.mainreply', '.parentReply');
            commentCollapse(this, '.childReplyCont', '.childReply');
        }
        $(this).data("clicks", !clicks);
    });

    $('.comReplyParent').click(function () {
        var clicks = $(this).data('clicks');
        var id = $(this).attr("id");
        var parentid = $('#' + id).closest('.commentExp').attr("id");
        if (!clicks) {
            $("#" + parentid + ">.collapseComment").show();
        }
        else {
            $('.collapseComment').hide();
        }
        $(this).data("clicks", !clicks);
    });

});

function commentExpand(target, parent, grandparent) {
    var id = $(target).attr("id");
    $("#" + id).html("+");
    $("#" + id).css("font-size", "16px");
    var parentid = $(target).closest(parent).attr('id');
    var expid = $("#" + parentid + "> .commentExp").attr("id");
    var grandparentid = $("#" + parentid).closest(grandparent).attr('id');
    var repliesid = $("#" + grandparentid + "> .commentreplies").attr("id");
    $("#" + expid).css("display", "none");
    $("#" + repliesid).css("display", "none");
}

function commentCollapse(target, parent, grandparent) {
    var id = $(target).attr("id");
    $("#" + id).html("&mdash;");
    $("#" + id).css("font-size", "10px");
    var parentid = $(target).closest(parent).attr('id');
    var expid = $("#" + parentid + "> .commentExp").attr("id");
    var grandparentid = $("#" + parentid).closest(grandparent).attr('id');
    var repliesid = $("#" + grandparentid + "> .commentreplies").attr("id");
    $("#" + expid).css("display", "block");
    $("#" + repliesid).css("display", "block");
}
