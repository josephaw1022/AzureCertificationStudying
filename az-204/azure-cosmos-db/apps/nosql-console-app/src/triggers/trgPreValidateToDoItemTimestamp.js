export default {
  id: 'trgPreValidateToDoItemTimestamp',
  triggerType: 'pre',            // 'pre' | 'post'
  triggerOperation: 'create',    // 'create' | 'replace' | 'delete' | 'all'
  body: function trgPreValidateToDoItemTimestamp() {
    var ctx = getContext();
    var req = ctx.getRequest();
    var doc = req.getBody();

    // If 'timestamp' missing or invalid, set to now
    if (!doc.timestamp || isNaN(Date.parse(doc.timestamp))) {
      doc.timestamp = new Date().toISOString();
    }
    req.setBody(doc);
  }
};
