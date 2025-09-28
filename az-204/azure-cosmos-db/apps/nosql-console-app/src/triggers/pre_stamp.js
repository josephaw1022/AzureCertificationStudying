export default {
  id: 'pre_stamp',
  triggerType: 'pre',
  triggerOperation: 'all',
  body: function pre_stamp() {
    var ctx = getContext();
    var req = ctx.getRequest();
    var doc = req.getBody();
    var now = new Date().toISOString();
    if (!doc.createdAt) doc.createdAt = now;
    doc.updatedAt = now;
    req.setBody(doc);
  }
};
