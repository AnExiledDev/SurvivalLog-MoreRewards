// Just enough of Root.html and the rewards page (RogueSelect.html) for page.js to run in Jint: documents, iframes,
// elements with classes, ids and inline style properties, the class and tag selectors the script uses, click events
// with a capture phase, the page's Vue state, the vuplex channel and timers the tests run by hand. Test code only;
// nothing here ships.
var window = this;
var location = { protocol: 'file:' };
var timers = [];
var timeouts = [];
var posted = [];

window.vuplex = { postMessage: function (message) { posted.push(message); } };

function setInterval(callback) {
    timers.push(callback);

    return timers.length;
}

function setTimeout(callback) {
    timeouts.push(callback);

    return timeouts.length;
}

function runTimers() {
    timers.forEach(function (callback) { callback(); });
}

function runTimeouts() {
    var due = timeouts;

    timeouts = [];
    due.forEach(function (callback) { callback(); });
}

var FakeElement = class {
    constructor(tagName, className) {
        var element = this;

        this.tagName = tagName;
        this.id = '';
        this.src = '';
        this.value = '';
        this.placeholder = '';
        this.hidden = false;
        this.ownerDocument = null;
        this.parentElement = null;
        this.children = [];
        this.listeners = [];
        this.text = '';
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

    get className() { return this.classes.join(' '); }

    set className(value) { this.classes = value ? value.split(' ').filter(function (c) { return c; }) : []; }

    get firstChild() { return this.children[0] || null; }

    // Like the DOM: the element's own text followed by its children's.
    get textContent() { return this.text + this.children.map(function (child) { return child.textContent; }).join(''); }

    set textContent(value) {
        this.children = [];
        this.text = String(value);
    }

    appendChild(child) {
        return this.insertBefore(child, null);
    }

    insertBefore(child, before) {
        if (child.parentElement) child.parentElement.removeChild(child);

        var at = before ? this.children.indexOf(before) : -1;

        child.parentElement = this;
        adopt(child, this.ownerDocument);

        if (at < 0) this.children.push(child);
        else this.children.splice(at, 0, child);

        return child;
    }

    removeChild(child) {
        this.children = this.children.filter(function (c) { return c !== child; });
        child.parentElement = null;

        return child;
    }

    remove() {
        if (this.parentElement) this.parentElement.removeChild(this);
    }

    removeChildren() {
        this.children = [];
    }

    matches(selector) {
        if (selector.charAt(0) === '.') return this.classList.contains(selector.slice(1));
        if (/^[a-z0-9]+$/.test(selector)) return this.tagName === selector;

        throw new Error('the fake page does not know the selector "' + selector + '"');
    }

    closest(selector) {
        for (var node = this; node; node = node.parentElement) {
            if (node.matches(selector)) return node;
        }

        return null;
    }

    querySelectorAll(selector) {
        return descendantsOf(this).filter(function (element) { return element.matches(selector); });
    }

    querySelector(selector) {
        return this.querySelectorAll(selector)[0] || null;
    }

    addEventListener(type, listener, capture) {
        this.listeners.push({ type: type, listener: listener, capture: !!capture });
    }
};

function adopt(element, doc) {
    element.ownerDocument = doc;
    element.children.forEach(function (child) { adopt(child, doc); });
}

function descendantsOf(root) {
    var found = [];

    root.children.forEach(function walk(child) {
        found.push(child);
        child.children.forEach(walk);
    });

    return found;
}

function makeDocument() {
    var listeners = [];
    var doc = {
        documentElement: new FakeElement('html'),
        getElementById: function (id) {
            return descendantsOf(doc.documentElement).filter(function (element) { return element.id === id; })[0] || null;
        },
        createElement: function (tagName) {
            var element = new FakeElement(tagName);

            element.ownerDocument = doc;

            return element;
        },
        querySelector: function (selector) { return doc.documentElement.querySelector(selector); },
        querySelectorAll: function (selector) { return doc.documentElement.querySelectorAll(selector); },
        addEventListener: function (type, listener, capture) { listeners.push({ type: type, listener: listener, capture: !!capture }); },
        listeners: listeners
    };

    doc.documentElement.ownerDocument = doc;
    doc.head = doc.documentElement.appendChild(new FakeElement('head'));
    doc.body = doc.documentElement.appendChild(new FakeElement('body'));

    return doc;
}

// A click (or key) as the browser sends it: the document's capture listeners first, then the target and each parent
// up to the root, until a listener stops it. Returns whether it reached the target's own listeners.
function fire(target, type, key) {
    var isStopped = false;
    var hasReachedTarget = false;
    var event = {
        type: type,
        key: key,
        target: target,
        stopPropagation: function () { isStopped = true; },
        preventDefault: function () {}
    };
    var doc = target.ownerDocument;

    doc.listeners.forEach(function (entry) {
        if (entry.type === type && entry.capture && !isStopped) entry.listener(event);
    });

    for (var node = target; node && !isStopped; node = node.parentElement) {
        if (node === target) hasReachedTarget = true;

        node.listeners.forEach(function (entry) {
            if (entry.type === type) entry.listener(event);
        });
    }

    return hasReachedTarget;
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
// with its .card inside, and the footer under the grid. Each card is { id, state, pinned }; the lock button shows on
// cards that are not bought or locked. The cards and points also go in the page's Vue state, where the production
// build keeps them (#app._vnode.component.setupState). Drawing again replaces the cards, as Vue does after a refresh.
function showRewards(frame, cards, points) {
    var doc = frame.contentDocument;
    var app = doc.getElementById('app');

    if (!app) {
        app = doc.body.appendChild(new FakeElement('div'));
        app.id = 'app';

        var content = app.appendChild(new FakeElement('div', 'panel')).appendChild(new FakeElement('div', 'content'));
        var footer = new FakeElement('div', 'footer');

        content.appendChild(new FakeElement('div', 'card-grid'));
        content.appendChild(footer);
        footer.appendChild(new FakeElement('div', 'footer-left')).appendChild(new FakeElement('span', 'own-num'));
        footer.appendChild(new FakeElement('div', 'footer-center'));
        footer.appendChild(new FakeElement('div', 'footer-right')).appendChild(new FakeElement('button', 'primary-btn'));
    }

    var grid = doc.querySelector('.card-grid');

    grid.removeChildren();
    cards.forEach(function (card) {
        var cardEl = grid.appendChild(new FakeElement('div', 'card-slot')).appendChild(new FakeElement('div', 'card ' + card.state));

        if (card.state !== 'bought' && card.state !== 'locked') cardEl.appendChild(new FakeElement('button', 'pin-btn')).appendChild(new FakeElement('span', 'pin-tip'));

        cardEl.appendChild(new FakeElement('div', 'card-name'));
        cardEl.appendChild(new FakeElement('button', 'card-btn'));
    });
    app._vnode = { component: { setupState: { cards: cards, ownPoints: points } } };
}

function showCards(frame, count) {
    var cards = [];

    for (var i = 0; i < count; i++) cards.push({ id: 30001 + i, state: 'affordable', pinned: false });

    showRewards(frame, cards, 0);
}

function stateOf(frame) {
    return frame.contentDocument.getElementById('app')._vnode.component.setupState;
}

function slotOf(frame, index) {
    return frame.contentDocument.querySelectorAll('.card-slot')[index];
}

function rootOf(frame) {
    return frame.contentDocument.documentElement;
}

function styleCount(frame) {
    return frame.contentDocument.head.children.filter(function (element) { return element.id === 'slmr-style'; }).length;
}

// The plugin's setup with every button on; a test overrides what it is about.
function setupWith(changes) {
    var data = { lang: 'en', maxLocked: 1, gamePin: 0, extras: [], ban: true, banCost: 2, labels: true, cashOut: 0, shop: true,
        shopIncrease: 15, shopReset: 'day', rate: 0.05, cap: 10, life: {} };

    Object.keys(changes || {}).forEach(function (key) { data[key] = changes[key]; });
    window.__slmorerewards.setup(data);
}

function shopRow(id, name, price, why, cat) {
    return { id: id, name: name, desc: name + ' does things', icon: '', cost: price, price: price, why: why || null, life: -1, cat: cat || 'body', early: false };
}
