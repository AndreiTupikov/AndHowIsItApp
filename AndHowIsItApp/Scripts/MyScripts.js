function changeCategory() {
    document.getElementById('subjectName').value = '';
    var category = document.getElementById('category').value;
    if (category === '1' | category === '2' | category === '3') autoCompleteSubjects(category);
    else autoCompleteSubjects(0);
}

function autoCompleteSubjects(category) {
    $("#subjectName").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/Home/GetSubjects?group=" + category,
                type: "POST",
                dataType: "json",
                data: { Prefix: request.term },
                success: function (data, textstatus) {
                    response($.map(data, function (item) {
                        return { label: item.Name, value: item.Name };
                    }))
                }
            })
        },
        messages: {
            noResults: "", results: ""
        }
    });
}

function autoCompleteTags() {
    $("#input-tag").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/Home/GetTags",
                type: "POST",
                dataType: "json",
                data: { Prefix: request.term },
                success: function (data, textstatus) {
                    response($.map(data, function (item) {
                        return { label: item.Name, value: item.Name };
                    }))
                }
            })
        },
        messages: {
            noResults: "", results: ""
        }
    });
}

function addTag() {
    const selectedTags = Array.prototype.slice.call(document.getElementById('selected-tags').children);
    const newTag = document.getElementById('input-tag').value;
    const contains = selectedTags.some(item => item.value === newTag)
    if (!contains && newTag.length > 0) {
        let element = document.createElement('input');
        element.setAttribute('type', 'text');
        element.setAttribute('name', 'tags');
        element.setAttribute('value', newTag)
        element.id = 'tag-' + newTag;
        element.setAttribute('onclick', 'removeTag(\''+element.id+'\')')
        document.getElementById('selected-tags').insertAdjacentElement("beforeend", element);
    }
    document.getElementById('input-tag').value = '';
}

function removeTag(id) {
    document.getElementById(id).remove();
}

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

//оценки для произведения и лайки для обзора и комментарии!
//Необходимо переделать чтобы применять только к некоторым страницам!!!
$(document).ready(function () {
    let subject = document.getElementById('subject-id').innerHTML;
    let review = document.getElementById('review-id').innerHTML;
    $.ajax({
        type: 'GET',
        url: '/Home/GetUserRating?subjectId=' + subject,
        success: function (data, textstatus) {
            $("#subjectRating-" + data).attr("checked", "checked");
        }
    });
    $.ajax({
        type: 'GET',
        url: '/Home/GetUserLike?reviewId=' + review,
        success: function (data, textstatus) {
            if (data === 'True') {
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

function sortResults() {
    let sortParam = document.getElementById('sortSelector').value.split('-');
    var previewSet = document.getElementsByName('previewSet');
    var previewArray = [].slice.call(previewSet).sort(function (a, b) {
        if (sortParam[0] === 'date') return dateFormatter(b.querySelector('#' + sortParam[0]).innerHTML) - dateFormatter(a.querySelector('#' + sortParam[0]).innerHTML);
        return b.querySelector('#' + sortParam[0]).innerHTML - a.querySelector('#' + sortParam[0]).innerHTML;
    });
    if (sortParam[1] === 'asc') previewArray.reverse();
    var results = document.getElementById('resultTable');
    previewArray.forEach(function (p) {
        results.appendChild(p);
    });
}

//переделать форматирование даты при переключении локализации
function dateFormatter(date) {
    var dateAndTime = date.split(' ');
    var date = dateAndTime[0].split('.');
    return new Date(date[2] + '-' + date[1] + '-' + date[0] + 'T' + dateAndTime[1]);
}