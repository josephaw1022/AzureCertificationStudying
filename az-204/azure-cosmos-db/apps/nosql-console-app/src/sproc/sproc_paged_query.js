export default {
  id: 'sproc_paged_query',
  body: function sproc_paged_query(continuationToken, pageSize) {
    var ctx = getContext(), coll = ctx.getCollection(), link = coll.getSelfLink();
    var q = 'SELECT * FROM c';
    var opts = { continuation: continuationToken, pageSize: pageSize };
    var acc = coll.queryDocuments(link, q, opts, function (err, docs, info) {
      if (err) throw err;
      ctx.getResponse().setBody({ items: docs, continuation: info.continuation });
    });
    if (!acc) return;
  }
};
