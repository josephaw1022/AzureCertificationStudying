export default {
  id: 'post_audit',
  triggerType: 'post',
  triggerOperation: 'create',
  body: function post_audit() {
    var ctx = getContext();
    var res = ctx.getResponse();
    // no-op; could inspect res.getBody() to log
  }
};
