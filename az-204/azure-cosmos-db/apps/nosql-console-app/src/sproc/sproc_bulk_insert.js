export default {
  id: 'sproc_bulk_insert',
  body: function sproc_bulk_insert(input) {
    var ctx = getContext();
    var coll = ctx.getCollection();
    var collLink = coll.getSelfLink();
    var docs = Array.isArray(input) ? input : JSON.parse(input || '[]');
    var i = 0, done = 0, total = docs.length;

    if (!total) { ctx.getResponse().setBody(0); return; }

    createNext();

    function createNext() {
      if (i >= total) { ctx.getResponse().setBody(done); return; }

      var doc = docs[i];               // pick exactly one
      var accepted = coll.createDocument(collLink, doc, {}, function (err) {
        if (err) {
          if (err.code === 409) {      // already exists -> skip and continue
            i++; done++;
            createNext();
            return;
          }
          throw err;                   // real error -> fail the sproc
        }
        i++;                           // advance ONLY after callback
        done++;
        createNext();
      });

      if (!accepted) {
        // engine asked us to yield; return partial count
        // (client can choose to call again if you ever batch large inputs)
        ctx.getResponse().setBody(done);
      }
    }
  }
};
