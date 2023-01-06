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

function sortResults() {
    let sortParam = document.getElementById('sortSelector').value.split('-');
    var previewSet = document.getElementsByName('preview-card');
    var sortedPreviews = [].slice.call(previewSet).sort(function (a, b) {
        if (sortParam[0] === 'date') return dateFormatter(b.querySelector('#' + sortParam[0]).innerHTML) - dateFormatter(a.querySelector('#' + sortParam[0]).innerHTML);
        if (sortParam[0] === 'subjectRating') return b.querySelector('#' + sortParam[0]).innerHTML.replaceAll(',', '.') - a.querySelector('#' + sortParam[0]).innerHTML.replaceAll(',', '.');
        return b.querySelector('#' + sortParam[0]).innerHTML - a.querySelector('#' + sortParam[0]).innerHTML;
    });
    if (sortParam[1] === 'asc') sortedPreviews.reverse();
    var results = document.getElementById('results-set');
    sortedPreviews.forEach(function (p) {
        results.appendChild(p);
    });
}

function dateFormatter(date) {
    var miliseconds = Date.parse(date);
    if (Number.isInteger(miliseconds)) return miliseconds;
    var dateAndTime = date.split(' ');
    var date = dateAndTime[0].split('.');
    return new Date(date[2] + '-' + date[1] + '-' + date[0] + 'T' + dateAndTime[1]);
}

$(window).ready(function () {
    let pictures = document.getElementsByName('picture') ?
        [...document.getElementsByName('picture')]
        : [];
    let uniqueIds = [];
    pictures.forEach((p) => {
        if (uniqueIds.includes(p.id)) return;
        uniqueIds.push(p.id);
        downloadPicture(p);
    })
});

function downloadPicture(picture) {
    $.ajax({
        type: 'GET',
        url: '/Home/DownloadPicture?path=' + picture.getAttribute('data-filepath') + '&postfix=' + picture.getAttribute('data-postfix'),
        success: function (data, textstatus) {
            if (data != '') {
                let elements = document.getElementsByName('picture');
                for (i = 0; i < elements.length; i++) {
                    if (elements[i].id === picture.id) {
                        elements[i].innerHTML = data;
                    }
                }
            }
        }
    });
}