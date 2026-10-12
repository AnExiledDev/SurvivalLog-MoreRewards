// Just enough of Root.html and the rewards page (RogueSelect.html) for page.js to run in Jint: documents, iframes,
// elements with classes, ids and inline style properties, the class and tag selectors the script uses, and timers the
// tests run by hand. Test code only; nothing here ships.
var window = this;
var location = { protocol: 'file:' };
var timers = [];

function setInterval(callback) {
    timers.push(callback);

    return timers.length;
}

function runTimers() {
    timers.forEach(function (callback) { callback(); });
}

var FakeElement = class {
    constructor(tagName, className) {
        var element = this;

        this.tagName = tagName;
        this.id = '';
        this.src = '';
        this.parentElement = null;
        this.children = [];
        this.textContent = '';
        this.classes = className ? className.split(' ') : [];
        this.styles = {};
        this.classList = {
            contains: function (name) { return element.classes.indexOf(name) >= 0; },
            add: function (name) { if (element.classes.indexOf(name) < 0) element.classes.push(name); },
            remove: function (name) { element.classes = element.classes.filter(function (c) { return c !== name; }); }
        };
        this.style = {
            getPropertyValue: function (name) { return element.styles[name] || ''; },
            setProperty: function (name, value) { element.styles[name] = value; }
        };
    }

    appendChild(child) {
        child.parentElement = this;
        this.children.push(child);

        return child;
    }

    removeChildren() {
        this.children = [];
    }

    matches(selector) {
        if (selector.charAt(0) === '.') return this.classList.contains(selector.slice(1));
        if (/^[a-z]+$/.test(selector)) return this.tagName === selector;

        throw new Error('the fake page does not know the selector "' + selector + '"');
    }

    querySelectorAll(selector) {
        return descendantsOf(this).filter(function (element) { return element.matches(selector); });
    }

    querySelector(selector) {
        return this.querySelectorAll(selector)[0] || null;
    }
};

function descendantsOf(root) {
    var found = [];

    root.children.forEach(function walk(child) {
        found.push(child);
        child.children.forEach(walk);
    });

    return found;
}

function makeDocument() {
    var doc = {
        documentElement: new FakeElement('html'),
        getElementById: function (id) {
            return descendantsOf(doc.documentElement).filter(function (element) { return element.id === id; })[0] || null;
        },
        createElement: function (tagName) { return new FakeElement(tagName); },
        querySelector: function (selector) { return doc.documentElement.querySelector(selector); },
        querySelectorAll: function (selector) { return doc.documentElement.querySelectorAll(selector); }
    };

    doc.head = doc.documentElement.appendChild(new FakeElement('head'));
    doc.body = doc.documentElement.appendChild(new FakeElement('body'));

    return doc;
}

var document = makeDocument();

// A page iframe in Root.html, as the game adds one: its src names the page.
function addFrame(src) {
    var frame = document.body.appendChild(new FakeElement('iframe'));

    frame.src = src;
    frame.contentDocument = makeDocument();

    return frame;
}

function addRewardsFrame() {
    return addFrame('file:///C:/Game/SurvivalLog_Data/StreamingAssets/WebUI/UI/RogueSelect/RogueSelect.html');
}

// A frame the browser will not let Root.html into.
function addBlockedRewardsFrame() {
    var frame = addRewardsFrame();

    Object.defineProperty(frame, 'contentDocument', { get: function () { throw new Error('blocked by the browser'); } });

    return frame;
}

// Draws the cards as RogueSelect's template does: #app > .panel > .content > .card-grid, one .card-slot per reward
// with its .card inside. Drawing again replaces the cards, as Vue does after a refresh.
function showCards(frame, count) {
    var doc = frame.contentDocument;
    var grid = doc.querySelector('.card-grid');

    if (!grid) {
        var app = doc.body.appendChild(new FakeElement('div'));
        var content = app.appendChild(new FakeElement('div', 'panel')).appendChild(new FakeElement('div', 'content'));

        app.id = 'app';
        grid = content.appendChild(new FakeElement('div', 'card-grid'));
    }

    grid.removeChildren();

    for (var i = 0; i < count; i++) grid.appendChild(new FakeElement('div', 'card-slot')).appendChild(new FakeElement('div', 'card'));
}

function rootOf(frame) {
    return frame.contentDocument.documentElement;
}

function styleCount(frame) {
    return frame.contentDocument.head.children.filter(function (element) { return element.id === 'slmr-style'; }).length;
}
