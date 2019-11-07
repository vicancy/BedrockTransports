"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var mqtt_1 = require("mqtt");
var client = mqtt_1.connect('ws://localhost:8080/ws/client/?hub=chat&subProtocol=mqtt&transferFormat=binary&access_token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjE1Njg5NjY3ODksImV4cCI6MTU2ODk3MDM4OSwiaWF0IjoxNTY4OTY2Nzg5LCJhdWQiOiJodHRwOi8vbG9jYWxob3N0L3dzL2NsaWVudC8_aHViPWNoYXQifQ.6Zyi0nEAVhVR4GBG-ZCMTnuGfz3z8Cso55JFtI8DnBM', {
    clientId: "client" + Math.floor(Math.random() * 6) + 1
});
window.onbeforeunload = function () {
    client.end();
};
var publishButton = document.getElementById("publish");
var topicInput = document.getElementById("topic");
var msgInput = document.getElementById("msg");
var stateParagraph = document.getElementById("state");
var msgsList = document.getElementById("msgs");
publishButton.onclick = function (click) {
    var topic = topicInput.value;
    var msg = msgInput.value;
    client.publish(topic, msg);
};
client.on('connect', function () {
    client.subscribe('#', { qos: 0 }, function (err, granted) {
        console.log(err);
    });
    client.publish('presence', 'Hello mqtt');
    stateParagraph.innerText = "connected";
    showMsg("[connect]");
});
client.on("error", function (e) {
    showMsg("error: " + e.message);
});
client.on("reconnect", function () {
    stateParagraph.innerText = "reconnecting";
    showMsg("[reconnect]");
});
client.on('message', function (topic, message) {
    showMsg(topic + ": " + message.toString());
});
function showMsg(msg) {
    //console.log(msg);
    var node = document.createElement("LI");
    node.appendChild(document.createTextNode(msg));
    msgsList.appendChild(node);
    if (msgsList.childElementCount > 50) {
        msgsList.removeChild(msgsList.childNodes[0]);
    }
}
//# sourceMappingURL=app.js.map