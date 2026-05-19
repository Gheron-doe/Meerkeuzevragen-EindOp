using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV_BL.Services;

public class TopicService
{
    private readonly ITopicRepository _repo;

    public TopicService(ITopicRepository repo)
    {
        _repo = repo;
    }

    public IReadOnlyList<Topic> GetAll(bool includeFlagged = false)
        => _repo.GetAll(includeFlagged);

    public Topic GetById(int id)
        => _repo.GetById(id) ?? throw new TopicNotFoundException(id);

    public int AddTopic(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new MeerkeuzevragenException("Topic name cannot be empty.");
        var topic = new Topic { Name = name.Trim(), Description = description, IsFlagged = false };
        return _repo.Add(topic);
    }
}
