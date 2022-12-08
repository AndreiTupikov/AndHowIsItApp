function changeGroup() {
    document.getElementById('subjectName').value = '';
    var group = document.getElementById('subjectGroup').value;
    if (group === '1' | group === '2' | group === '3') autoCompleteSubjects(group);
    else autoCompleteSubjects(0);
}

function autoCompleteSubjects(group) {
    $("#subjectName").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/Home/GetSubjects?group=" + group,
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
    const contains = selectedTags.some(item => item.innerHTML === newTag)
    if (!contains && newTag.length > 0) {
        let element = document.createElement('div');
        element.id = 'tag-' + newTag;
        element.innerHTML = newTag;
        element.setAttribute('onclick', 'removeTag(\''+element.id+'\')')
        document.getElementById('selected-tags').insertAdjacentElement("beforeend", element);
    }
    document.getElementById('input-tag').value = '';
}

function removeTag(id) {
    document.getElementById(id).remove();
}

function addAllTags() {
    let result = '';
    const selectedTags = document.getElementById('selected-tags').children;
    for (i = 0; i < selectedTags.length; i++) {
        result += selectedTags[i].innerHTML + '|';
    }
    document.getElementById('model-tags').value = result;
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

$(document).ready(function () {
    let subject = document.getElementById('subject-id').innerHTML;
    console.log(subject)
    $.ajax({
        type: 'GET',
        url: '/Home/GetUserRating?subjectId=' + subject,
        success: function (data, textstatus) {
            console.log(data)
            $("#subjectRating-" + data).attr("checked", "checked");
        }
    });
});

