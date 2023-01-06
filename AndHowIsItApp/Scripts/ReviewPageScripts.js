function rateSubject(rating) {
    let subject = document.getElementById('subject-id').innerHTML;
    $.ajax({
        type: 'GET',
        url: '/Home/RateSubject?subjectId=' + subject + '&rating=' + rating,
        success: function (data, textstatus) {
            $("#usersRating").html(data);
        }
    });
}

function likeReview() {
    let review = document.getElementById('review-id').innerHTML;
    $.ajax({
        type: 'GET',
        url: '/Home/LikeReview?reviewId=' + review,
        success: function (data, textstatus) {
            $("#allReviewLikes").html(data);
        }
    });
}

$(document).ready(function () {
    let subject = document.getElementById('subject-id').innerHTML;
    let review = document.getElementById('review-id').innerHTML;
    $.ajax({
        type: 'GET',
        url: '/Home/GetUserRating?subjectId=' + subject,
        success: function (data, textstatus) {
            let rating = removeServerCreatedScript(data);
            $("#subjectRating-" + rating).attr("checked", "checked");
        }
    });
    $.ajax({
        type: 'GET',
        url: '/Home/GetUserLike?reviewId=' + review,
        success: function (data, textstatus) {
            if (removeServerCreatedScript(data) === 'True') {
                $("#reviewLike").attr("checked", "checked");
            }
        }
    });
    getCommentsUpdate = function () {
        $.ajax({
            type: 'GET',
            url: '/Home/GetComments?reviewId=' + review,
            success: function (data, textstatus) {
                if (data != '') {
                    $("#allComments").html(data);
                }
            }
        });
    }
    setInterval(getCommentsUpdate, 5000);
});

function removeServerCreatedScript(response) {
    if (response.includes('<!--SCRIPT')) {
        return response.substring(0, response.indexOf('<!--SCRIPT'));
    }
    return response;
}